using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
    class TestSettings
    {
        public struct Setting
        {
            public readonly string fen;
            public readonly int numOfMoves;
            public readonly int numOfPlayouts;
            public readonly int playoutLength;
            public readonly bool team;


            public Setting(string fen, int numOfMoves, int numOfPlayouts, int playoutLength, bool team)
            {
                this.fen = fen;
                this.numOfMoves = numOfMoves;
                this.numOfPlayouts = numOfPlayouts;
                this.playoutLength = playoutLength;
                this.team = team;
            }
        }

        public static List<Setting> tests = new List<Setting>
        {
            // 1 move
            new Setting("6k1/1p3p1p/2q2Qpb/8/3B4/2P3PP/5P1K/8 w - - 0 1", 1, 300, 3, true),
            new Setting("2k5/ppp2p2/7q/6p1/2Nb1p2/1B3Kn1/PP2Q1P1/8 b - - 0 1", 1, 1000, 3, false),
            new Setting("4r1rk/5p2/3p4/R7/6R1/7P/7b/2BK4 w - - 0 1", 1, 300, 3, true),
            new Setting("8/R7/4pk1p/3rN1p1/3P4/6P1/2n3KP/8 w - - 0 1", 1, 700, 3, true),
            new Setting("1q2r3/3kPpp1/1p1P1b1p/3Q1P2/1p6/P6P/1P4P1/1KR2b2 w - - 0 1", 1, 500, 3, true),

            // 2 moves
            new Setting("r2qk2r/pb4pp/1n2Pb2/2B2Q2/p1p5/2P5/2B2PPP/1N4K1 w - - 1 0", 2, 7000, 5, true), //Winning sequence Queen G6, Pawn G6, Bishop G6
            new Setting("6k1/pp4p1/2p5/2bp4/8/P5Pb/1P3rrP/2BRRN1K b - - 0 1", 2, 3000, 5, false),       //-||- Rook G1, king G1, Rook G2
                                                                                                        //Trace this move, see if it's detection errors for checkmate via blocked moves

            new Setting("8/2k2p2/2b3p1/P1p1Np2/1p3b2/1P1K4/5r2/R3R3 b - - 0 1", 2, 5000, 5, false),     //Bishop b5, Knight c4, Rook d2
            new Setting("6k1/5p2/1p5p/p4Np1/5q2/Q6P/PPr5/3R3K w - - 1 0", 2, 7000, 5, true),            //Queen f8, King h7 , Queen g7 or -||- king f8, rook d8
            new Setting("r1b2k1r/ppppq3/5N1p/4P2Q/4PP2/1B6/PP5P/n2K2R1 w - - 1 0", 2, 40000, 5, true),  //q h6 and either r h6 rg8 or q g7 q g7
            
            // 3 moves
            new Setting("1k5r/pP3ppp/3p2b1/1BN1n3/1Q2P3/P1B5/KP3P1P/7q w - - 1 0", 3, 45000, 7, true),//Many good lines exist, but checkmate end via blocked moves
            new Setting("3r1r1k/1p3p1p/p2p4/4n1NN/6bQ/1BPq4/P3p1PP/1R5K w - - 1 0", 3, 4000, 7, true),
            new Setting("R6R/1r3pp1/4p1kp/3pP3/1r2qPP1/7P/1P1Q3K/8 w - - 1 0", 3, 20000, 7, true),
            new Setting("4r1k1/5bpp/2p5/3pr3/8/1B3pPq/PPR2P2/2R2QK1 b - - 0 1", 3, 45000, 7, false),
            new Setting("2r5/2p2k1p/pqp1RB2/2r5/PbQ2N2/1P3PP1/2P3P1/4R2K w - - 1 0", 3, 400000, 7, true),

            // 4 moves
            /*new Setting("7R/r1p1q1pp/3k4/1p1n1Q2/3N4/8/1PP2PPP/2B3K1 w - - 1 0", 4, 0, 0, true),
            new Setting("Q7/p1p1q1pk/3p2rp/4n3/3bP3/7b/PP3PPK/R1B2R2 b - - 0 1", 4, 0, 0, false),
            new Setting("4k2r/1R3R2/p3p1pp/4b3/1BnNr3/8/P1P5/5K2 w - - 1 0", 4, 0, 0, true),
            new Setting("r2r1n2/pp2bk2/2p1p2p/3q4/3PN1QP/2P3R1/P4PP1/5RK1 w - - 0 1", 4, 0, 0, true),
            new Setting("3r1r2/1pp2p1k/p5pp/4P3/2nP3R/2P3QP/P1B1q1P1/5RK1 w - - 1 0", 4, 0, 0, true),*/

            // 5 moves
            /*new Setting("6k1/3b3r/1p1p4/p1n2p2/1PPNpP1q/P3Q1p1/1R1RB1P1/5K2 b - - 0 1", 5, 0, 0, false),
            new Setting("2q1nk1r/4Rp2/1ppp1P2/6Pp/3p1B2/3P3P/PPP1Q3/6K1 w - - 1 0", 5, 0, 0, true),
            new Setting("6r1/p3p1rk/1p1pPp1p/q3n2R/4P3/3BR2P/PPP2QP1/7K w - - 1 0", 5, 0, 0, true),
            new Setting("4r3/7q/nb2prRp/pk1p3P/3P4/P7/1P2N1P1/1K1B1N2 w - - 1 0", 5, 0, 0, true),
            new Setting("1k4r1/1p4r1/PPp5/2bbN3/4np1p/7P/6PK/R1R1BB2 b - - 0 1", 5, 0, 0, false),*/
        };
    }
}