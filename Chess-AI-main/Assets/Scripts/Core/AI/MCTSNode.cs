namespace Chess
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class MCTSNode
    {
        //this gives me who moves next
        public bool isBlackMove;

        //We track all these since outside pure MCTS we can eval draws different.
        public int wonPlayouts=0;
        public int lostPlayouts=0;
        public int drawPlayouts=0;

        public MCTSNode prevNode;//My back-propagation hook if I don't wanna do recursion.

        public int depth;//Depth is necessary if we want to adjust the balance of exploration of deep vs. unexplored moves.

        public Board boardState;//I'd rather use a different one since this doesn't give board ambiguity between moves, but that just means it's a tree, not DAG (Both should work)
        public Move lastMove;//Technically gettable from boardState, but I would at least want it as a getter method for ease of access.

        //ADDED FOR SEARCH:
        public Dictionary<Move, MCTSNode> children;

        //public List<Move> validMoves; //This is used to store precomputed moves possible with a given board. Comparing if a move is in here or not would also give us unvisited moves.

        public bool isTerminal;//Is this an end-state?

        public MCTSNode(bool isBlackMove, MCTSNode prevNode, int depth, Board boardState, Move lastMove)
        {
            this.isBlackMove = isBlackMove;
            this.prevNode = prevNode;
            this.depth = depth;
            this.boardState = boardState;
            this.lastMove = lastMove;

            children = new Dictionary<Move, MCTSNode>();
            //validMoves = new List<Move>();
            //TODO: Add isTerminal as a thing.
        }

        //Non-necessary items:

        //Since simulation steps can possibly be done by a directing method rather than in node, we don't necessarily "need" the simulation of next step here (Actually, not having it is better)
        //Same goes for any selection weighting since the necessary information is provided. But it might be more efficient to cache it here.



    }
}