namespace Chess
{
    using Chess.Game;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using UnityEngine;
    using static System.Math;

    public enum ResultAbridged { WhiteWin, WhiteLoss, Draw }

    class MCTSSearch : ISearch
    {
        public event System.Action<Move> onSearchComplete;

        const double drawWinMult = 0.5;

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
            MCTSNode rootNode = new MCTSNode(!board.WhiteToMove, null, board, Move.InvalidMove);
            int counter = 0;
            do
            {
                if (settings.limitNumOfPlayouts)
                {
                    if (counter >= settings.maxNumOfPlayouts)
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

                BackpropagateFromNode(expandingNode, result);//BUG: This sucks! I backpropagate from incorrect colors on occasion.

            } while (!abortSearch);
            //TODO: Add a search through all children of Root for one with most won playouts
            var maxScore = 0.0f;
            Move tempBestMove=Move.InvalidMove;
            foreach (var kvp in rootNode.children)
            {
                float score = kvp.Value.totalPlayoutScore/ kvp.Value.playouts;
                if (score > maxScore)
                {
                    maxScore = score;
                    tempBestMove = kvp.Key;
                }
            }
            bestMove=tempBestMove;
            return;
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
                        int totalPlayoutsParent = node.playouts;
                        int totalPlayoutsChild = child.playouts;
                        double currentUCB;
                        if (totalPlayoutsChild > 0)
                        {
                            currentUCB = child.totalPlayoutScore / totalPlayoutsChild + explorationParam * Mathf.Sqrt(Mathf.Log(totalPlayoutsParent) / totalPlayoutsChild);//TODO: Unsure if this is the right UCB calculation
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
            //if (!node.isTerminal) {
                MoveGenerator moveGenerator = new MoveGenerator();

                List<Move> children = moveGenerator.GenerateMoves(node.boardState, true, true);

                if (children.Count == 0)
                {
                    node.isTerminal = true;//Only expanded nodes can be considered terminal. This means we cycle into it one more time, but should still allow us to correctly execute with little efficiency loss
                    return;
                }
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    Board boardCopy = node.boardState.Clone();
                    boardCopy.MakeMove(children[i], true);//Now we have the move.                
                    MCTSNode newNode = new MCTSNode(boardCopy.ColourToMove == Piece.Black, node, boardCopy, children[i]);
                    node.children.Add(children[i], newNode);
                }
        }

        
        static float SimulateFromNode(MCTSNode node, int maxSimulatedMoves = 50)
        {
                //Simulated moves are capped at a reasonable future value (Stockfish search depths usually cap out near the 30s even on modern PCs and that is already 99%+ accurate)
                var board = node.boardState.GetLightweightClone();
                int movesTaken = 0;
                bool whiteToMove = node.boardState.WhiteToMove;
                MoveGenerator moveGenerator = new MoveGenerator();
                var moves = moveGenerator.GetSimMoves(board, true);
                //While there are moves available, go and randomly select a move and move on the board.
                int fiftyMoveCounter = node.boardState.fiftyMoveCounter;
                bool whiteDead;
                bool kingMissing = oneKingDead(board, out whiteDead);
                while (movesTaken < maxSimulatedMoves && !kingMissing)
                {
                    movesTaken++;
                    int nextMoveIndex = UnityEngine.Random.Range(0, moves.Count);
                    if (MakeSimMove(moves[nextMoveIndex], ref board))
                    {
                        fiftyMoveCounter = 0;
                    }
                    else
                    {
                        fiftyMoveCounter++;
                    }
                    whiteToMove = !whiteToMove;
                    kingMissing = oneKingDead(board, out whiteDead);
                    moves = moveGenerator.GetSimMoves(board, false);
                    if (fiftyMoveCounter > 50) break;//kill if we'd draw anyway
                }
                Evaluation evaluation = new Evaluation();
                return evaluation.EvaluateSimBoard(board, !node.boardState.WhiteToMove);//HACK: This had to be negated due to the while cycle, watch for it again.
        }
        static void BackpropagateFromNode(MCTSNode node, float result)
        {
            //Recursive calling of backpropagation on the tree... Runs into the recursion limit potentially, but that shouldn't be an issue in practice due to the branching factor of playouts.
            node.playouts++;
            node.totalPlayoutScore += result;
            if (node.prevNode != null)
            {
                BackpropagateFromNode(node.prevNode, 1-result);//TODO: Probably better to use the actual chess material difference instead of just results enum
            }
        }

        //Makes a move assuming the simMove is valid (startPos is nonempty and endPos is a valid move) 
        static bool MakeSimMove(SimMove simMove, ref SimPiece[,] boardClone)
        {
            bool tookAPiece = boardClone[simMove.endCoord1, simMove.endCoord2]!=null;
            boardClone[simMove.endCoord1, simMove.endCoord2] = boardClone[simMove.startCoord1, simMove.startCoord2];
            boardClone[simMove.startCoord1, simMove.startCoord2] = null;
            return tookAPiece;
        }
        //This has to be added since a dead king is possible.
        static bool oneKingDead(SimPiece[,] board, out bool whiteIsMissing)
        {
            bool whiteExists = false;
            bool blackExists = false;
            for (int i = 0; i < board.GetLength(0); i++)
            {
                for (int j = 0; j < board.GetLength(1); j++)
                {
                    if (board[i,j] != null)
                    {
                        if (board[i, j].type == SimPieceType.King)
                        {
                            if (board[i, j].team)
                            {
                                whiteExists = true;
                            }
                            else
                            {
                                blackExists = true;
                            }
                        }
                    }
                }
            }
            whiteIsMissing=!whiteExists;
            return !(whiteExists && blackExists);
        }
    }
}