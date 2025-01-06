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

            throw new NotImplementedException();
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

            while (0 == node.children.Count && !node.isTerminal)
            {
                //TODO:Select on UCB
                double maxUCB = 0;
                MCTSNode nextNode = null;
                foreach (var kvp in node.children)
                {
                    MCTSNode child = kvp.Value;
                    int totalPlayoutsParent = node.wonPlayouts + node.drawPlayouts + node.lostPlayouts;
                    int totalPlayoutsChild = child.wonPlayouts + child.drawPlayouts + child.lostPlayouts;
                    double currentUCB =
                        (child.wonPlayouts + child.drawPlayouts * drawWinMult) / (totalPlayoutsChild) +
                        explorationParam * Mathf.Sqrt(Mathf.Log(totalPlayoutsParent) / totalPlayoutsChild);//TODO: Unsure if this is the right UCB calculation

                    if (currentUCB > maxUCB)
                    {
                        maxUCB = currentUCB;
                        nextNode = child;
                    }
                }
                node = nextNode;
            }
            if (node.isTerminal)
            {
                return node.prevNode;//TODO: We need to modify this for the terminal node presence!!!
            }
            else return node;

            //I think creating a node and doing an expansion is the next homework, but I'm not sure.

        }
        static void expandNode(MCTSNode node)
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
                children=moveGenerator.GenerateMoves(node.boardState, false, true);
            }
            for (int i = children.Count - 1; i >= 0; i--)
            {
                Board boardCopy = node.boardState.Clone();
                boardCopy.MakeMove(children[i], true);//Now we have the move.

                MCTSNode newNode = new MCTSNode(boardCopy.ColourToMove == Piece.Black, node, node.depth + 1, boardCopy, children[i]);

                if (GetResultFromBoard(boardCopy) != Result.Playing)
                {
                    newNode.isTerminal = true;
                }
                node.children.Add(children[i], newNode);
            }
        }
        
        //Following section is copied from the game manager since there was no other way to access the evaluation.
        public enum Result { Playing, WhiteIsMated, BlackIsMated, Stalemate, Repetition, FiftyMoveRule, InsufficientMaterial, TooManyMoves }
        static Result GetResultFromBoard(Board board)
        {
            MoveGenerator moveGenerator = new MoveGenerator();
            var moves = moveGenerator.GenerateMoves_DO_NOT_USE(board);

            // Look for mate/stalemate
            if (moves.Count == 0)
            {
                if (moveGenerator.InCheck())
                {
                    return (board.WhiteToMove) ? Result.WhiteIsMated : Result.BlackIsMated;
                }
                return Result.Stalemate;
            }

            // Fifty move rule
            if (board.fiftyMoveCounter >= 100)
            {
                return Result.FiftyMoveRule;
            }

            // Threefold repetition
            int repCount = board.RepetitionPositionHistory.Count((x => x == board.ZobristKey));
            if (repCount == 3)
            {
                return Result.Repetition;
            }

            // Look for insufficient material (not all cases implemented yet)
            int numPawns = board.pawns[Board.WhiteIndex].Count + board.pawns[Board.BlackIndex].Count;
            int numRooks = board.rooks[Board.WhiteIndex].Count + board.rooks[Board.BlackIndex].Count;
            int numQueens = board.queens[Board.WhiteIndex].Count + board.queens[Board.BlackIndex].Count;
            int numKnights = board.knights[Board.WhiteIndex].Count + board.knights[Board.BlackIndex].Count;
            int numBishops = board.bishops[Board.WhiteIndex].Count + board.bishops[Board.BlackIndex].Count;

            if (numPawns + numRooks + numQueens == 0)
            {
                if (numKnights == 1 || numBishops == 1)
                {
                    return Result.InsufficientMaterial;
                }
            }

            return Result.Playing;
        }
    
    }
}