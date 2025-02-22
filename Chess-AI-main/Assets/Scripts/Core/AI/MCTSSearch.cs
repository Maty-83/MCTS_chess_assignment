namespace Chess
{
    using Chess.Game;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using UnityEngine;
    using static System.Math;
    class MCTSSearch : ISearch
    {
        public event System.Action<Move> onSearchComplete;

        MoveGenerator moveGenerator;

        Move bestMove;
        int bestEval;
        bool abortSearch;

        MCTSSettings settings;
        Board board;
        Evaluation evaluation;

        System.Random rand;

        // Diagnostics
        public SearchDiagnostics Diagnostics { get; set; }
        System.Diagnostics.Stopwatch searchStopwatch;

        public MCTSSearch(Board board, MCTSSettings settings)
        {
            this.board = board;
            this.settings = settings;
            evaluation = new Evaluation();
            moveGenerator = new MoveGenerator();
            rand = new System.Random();
        }

        public void StartSearch()
        {
            InitDebugInfo();

            // Initialize search settings
            bestEval = 0;
            bestMove = Move.InvalidMove;

            moveGenerator.promotionsToGenerate = settings.promotionsToSearch;
            abortSearch = false;
            Diagnostics = new SearchDiagnostics();

            SearchMoves();

            onSearchComplete?.Invoke(bestMove);

            if (!settings.useThreading)
            {
                LogDebugInfo();
            }
        }

        public void EndSearch()
        {
            if (settings.useTimeLimit)
            {
                abortSearch = true;
            }
        }

        void SearchMoves()
        {
            // TODO
            // Don't forget to end the search once the abortSearch parameter gets set to true.

            //throw new NotImplementedException();
            MCTSNode rootNode = new MCTSNode(board.WhiteToMove, null, board.GetLightweightClone(), new SimMove());
            int counter=0;
            do
            {
                if (settings.limitNumOfPlayouts)
                {
                    if (!(settings.maxNumOfPlayouts > counter))
                    {
                        break;
                    }
                    counter++;
                }

                //Search loop here. Do-while as a simple way to make sure we always create a valid outcome so I'm not solving any stupid things.
                MCTSNode expandingNode = DoSelection(rootNode); //HACK: We assume root isn't terminal, unsure if it's right.
                ExpandNode(expandingNode);
                var result = SimulateFromNode(expandingNode, settings.playoutDepthLimit);   //WARNING: Is this how it's supposed to go? Not sure, we're gonna skip the final layer.
                                                                //Note: The simulation on a terminal node happens on the first expansion.
                BackpropagateFromNode(expandingNode, result);                

            } while (!abortSearch);
            //TODO: Add a search through all children of Root for one with most won playouts
            var maxWonPlayoutsRatio = -0.01;
            SimMove bestSimMove;
            foreach (var kvp in rootNode.children)
            {
                double winRatio = kvp.Value.wonPlayouts / (kvp.Value.wonPlayouts + kvp.Value.drawPlayouts + kvp.Value.lostPlayouts);
                if (winRatio > maxWonPlayoutsRatio)
                {
                    maxWonPlayoutsRatio = winRatio;
                    bestSimMove = kvp.Key;
                }
            }
            bestMove=new Move()
            //throw new NotImplementedException();//Because it hasn't been tested and the feedback isn't incorporated yet.
        }

        void LogDebugInfo()
        {
            // Optional
        }

        void InitDebugInfo()
        {
            searchStopwatch = System.Diagnostics.Stopwatch.StartNew();
            // Optional
        }

        static MCTSNode DoSelection(MCTSNode root)
        {
            //Worst-case I have to wrap this somewhere, but for now we do it this way

            //While we haven't reached a node with 0 children: Select node via UCB from done playouts and insert score of incomplete nodes.
            //If the UCB of an unexplored node is higher, select a random node from the unexplored nodes.

            const double drawWinMult = 0.5;
            double explorationParam = 1;
            var node = root;

            while (0 != node.children.Count && !node.isTerminal)
            {
                //TODO:Select on UCB
                double maxUCB = 0;
                MCTSNode nextNode = null;
                foreach (var kvp in node.children)
                {
                    MCTSNode child = kvp.Value;
                    //if (!child.isTerminal)//We don't expand terminals further since their outcome is known. //TODO: Is this correct or do we select anyway?
                    //{
                        int totalPlayoutsParent = node.wonPlayouts + node.drawPlayouts + node.lostPlayouts;
                        int totalPlayoutsChild = child.wonPlayouts + child.drawPlayouts + child.lostPlayouts;
                        double currentUCB;
                        if (totalPlayoutsChild > 0)
                        {
                            currentUCB = (child.wonPlayouts + child.drawPlayouts * drawWinMult) / (totalPlayoutsChild) +
                            explorationParam * Mathf.Sqrt(Mathf.Log(totalPlayoutsParent) / totalPlayoutsChild);//TODO: Unsure if this is the right UCB calculation
                        }
                        else {
                            currentUCB = double.MaxValue;//This is *bad*, but a good enough approximation of UCB values which are infinity
                        }

                        if (currentUCB > maxUCB)
                        {
                            maxUCB = currentUCB;
                            nextNode = child;
                        }
                    //}
                }
                node = nextNode;
            }
            //Since we know we haven't gone into any known terminal node, we've returned either an non-terminal node or an unknown terminal node the expansion step will immediately catch.
            return node;

        }
        static void ExpandNode(MCTSNode node)
        {
            //I will probably write a governing function later, so this doesn't need to be connected yet.

            MoveGenerator moveGenerator = new MoveGenerator();

            List<Move> children;
            if (node.prevNode == null)
            {
                children = moveGenerator.GenerateMoves(node.boardState, true, true);
            }
            else
            {
                children = moveGenerator.GenerateMoves(node.boardState, false, true);//TODO: Check with Petr that I'm actually supposed to ignore illegal moves in children even for expansion, not just sim.
            }
            if (children.Count == 0)
            {
                node.isTerminal = true;//Only expanded nodes can be considered terminal. This means we cycle into it one more time, but should still allow us to correctly execute with little efficiency loss
                return;
            }
            for (int i = children.Count - 1; i >= 0; i--)
            {
                Board boardCopy = node.boardState.Clone();
                boardCopy.MakeMove(children[i], true);//Now we have the move.

                MCTSNode newNode = new MCTSNode(!node.WhiteToMove, node, boardCopy, children[i]);
                node.children.Add(children[i], newNode);
            }
        }

        enum ResultAbridged { WhiteWin, WhiteLoss, Draw }
        static ResultAbridged SimulateFromNode(MCTSNode node, int maxSimulatedMoves = 50)
        {
            //Simulated moves are capped at a reasonable future value (Stockfish search depths usually cap out near the 30s even on modern PCs and that is already 99%+ accurate)
            Board board = node.boardState.Clone();
            int movesTaken = 0;
            MoveGenerator moveGenerator = new MoveGenerator();
            var moves = moveGenerator.GenerateMoves(board, true);
            //While there are moves available, go and randomly select a move and move on the board.
            while (moves.Count > 0 && movesTaken < maxSimulatedMoves)
            {
                
                int nextMoveIndex = UnityEngine.Random.Range(0, moves.Count);
                //Debug.Log("Selected Move: " + moves[nextMoveIndex].StartSquare + " to " + moves[nextMoveIndex].TargetSquare);
                board.MakeMove(moves[nextMoveIndex], true);//Something seems to break here? Got an out-of-bounds exception for a move following the stack trace from this.
                movesTaken++;

                if (board.fiftyMoveCounter > 50) {
                    //Debug.Log("Sim round end");
                    return ResultAbridged.Draw;//kill if we'd draw anyway.                    
                   }

                moves = moveGenerator.GenerateMoves(board, false);
            }
            //Once there are no moves or we reached the end, we evaluate the position
            //Debug.Log("Sim round end");
            if (moves.Count == 0)
            {
                //Termination on no available moves. Current on move loses.
                if (board.WhiteToMove) return ResultAbridged.WhiteLoss;
                else return ResultAbridged.WhiteWin;
            }
            else
            {
                //Assumes we stopped for a good reason, we call it a draw.
                return ResultAbridged.Draw;
            }

        }
        static void BackpropagateFromNode(MCTSNode node, ResultAbridged result)
        {
            //Recursive calling of backpropagation on the tree... Runs into the recursion limit potentially, but that shouldn't be an issue in practice due to the branching factor of playouts.
            switch (result)
            {
                //This might need some adjusting
                case ResultAbridged.WhiteWin:
                    if (node.WhiteToMove)
                    {
                        node.wonPlayouts++;
                    }
                    else
                    {
                        node.lostPlayouts++;
                    }
                    break;
                case ResultAbridged.WhiteLoss:
                    if (node.WhiteToMove)
                    {
                        node.lostPlayouts++;
                    }
                    else
                    {
                        node.wonPlayouts++;
                    }
                    break;
                case ResultAbridged.Draw:
                    node.drawPlayouts++;
                    break;
                default:
                    throw new Exception("Unexpected result");
            }
            if (node.prevNode != null)
            {
                BackpropagateFromNode(node.prevNode, result);//TODO: Probably better to use the actual chess material difference instead of just results enum
            }
        }

    }
}