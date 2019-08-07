using System.Collections.Generic;

namespace Vertigo.Core
{
    // A match on the grid, it can consist of any number of hexagon pieces but these pieces are
    // guaranteed to be adjacent to each other and have the same color
    public class HexagonMatch
    {
        private readonly List<HexagonPiece> _matches = new List<HexagonPiece>(6);
        public int Count => _matches.Count;
        public HexagonPiece this[int index] => _matches[index];

        public void Add(HexagonPiece piece) => _matches.Add(piece);

        public void Add(HexagonGroup Group)
        {
            _matches.Add(Group.Piece1);
            _matches.Add(Group.Piece2);
            _matches.Add(Group.Piece3);
        }

        public void Clear() => _matches.Clear();
    }
}