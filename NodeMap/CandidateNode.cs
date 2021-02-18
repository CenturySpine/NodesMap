using System.Numerics;

namespace NodeMap
{
    public class CandidateNode
    {
        public Vector2 Position;
        public float F_Score { get; set; }
        public float H_Score { get; set; }
        public int G_Score { get; set; }

        public CandidateNode Parent { get; set; }

        public override string ToString()
        {
            return $"{nameof(Position)}: {Position}, {nameof(F_Score)}: {F_Score}, {nameof(H_Score)}: {H_Score}";
        }
    }
}