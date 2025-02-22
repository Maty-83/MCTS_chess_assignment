namespace Chess
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class MCTSNode
    {
        //this gives me who is on the move.
        public bool WhiteToMove;

        //We track all these since outside pure MCTS we can eval draws different.
        public int wonPlayouts=0;
        public int lostPlayouts=0;
        public int drawPlayouts=0;

        public MCTSNode prevNode;//My back-propagation hook if I don't wanna do recursion.

        public SimPiece[,] boardState;//I'd rather use a different one since this doesn't give board ambiguity between moves, but that just means it's a tree, not DAG (Both should work)
        //TODO: Investigate using board clone for move generator
        
        public SimMove lastMove;//Technically gettable from boardState, but I would at least want it as a getter method for ease of access.
        
        //ADDED FOR SEARCH:
        public Dictionary<SimMove, MCTSNode> children;

        //public List<Move> validMoves; //This is used to store precomputed moves possible with a given board. Comparing if a move is in here or not would also give us unvisited moves.

        public bool isTerminal;//Is this an end-state?

        public MCTSNode(bool WhiteToMove, MCTSNode prevNode, SimPiece[,] boardState, SimMove lastMove)
        {
            this.WhiteToMove = WhiteToMove;
            this.prevNode = prevNode;
            this.boardState = boardState;
            this.lastMove = lastMove;
            children = new Dictionary<SimMove, MCTSNode>();
            //validMoves = new List<Move>();
            //TODO: Add isTerminal as a thing.
        }

        //Non-necessary items:
        //Selection comparator, not necessary but perhaps decent.



    }
}