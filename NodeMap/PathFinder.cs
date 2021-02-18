using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace NodeMap
{
    public interface IAStarHeuristic
    {
        float Compute(Vector2 start, Vector2 end, List<Vector2> forbiden, List<Rect> obstacles,
            Dictionary<Vector2, int> redundency);
    }

    public class NoHeuristic : IAStarHeuristic
    {
        public float Compute(Vector2 start, Vector2 end, List<Vector2> forbiden, List<Rect> obstacles,
            Dictionary<Vector2, int> redundency)
        {
            return 0;
        }
    }

    public class CurrentToEndAbsoluteDistance : IAStarHeuristic
    {
        public float Compute(Vector2 start, Vector2 end, List<Vector2> forbiden, List<Rect> obstacles,
            Dictionary<Vector2, int> redundency)
        {
            return Math.Abs(end.X - start.X) + Math.Abs(end.Y - start.Y);
        }
    }
    public class CurrentToEndAbsoluteDistanceWithRedundency : IAStarHeuristic
    {
        private readonly float _modifier;

        public CurrentToEndAbsoluteDistanceWithRedundency(float modifier)
        {
            _modifier = modifier;
        }
        public float Compute(Vector2 start, Vector2 end, List<Vector2> forbiden, List<Rect> obstacles,
            Dictionary<Vector2, int> redundency)
        {
            float intersectModifier = 0.0f;
            if (redundency.TryGetValue(start, out var red))
            {
                intersectModifier = (red-1) * _modifier;
            }
            //foreach (var obstacle in obstacles)
            //{
            //    bool intersect = IntersectHelpers.LineIntersectsRect(new Point(start.X, start.Y), new Point(end.X, end.Y), obstacle);
            //    if (intersect)
            //        intersectModifier += _modifier;
            //}
            return Math.Abs(end.X - start.X) + Math.Abs(end.Y - start.Y) + intersectModifier;
        }
    }
    public class CurrentToEndAbsoluteDistanceWithLineOfSight : IAStarHeuristic
    {
        private readonly float _modifier;

        public CurrentToEndAbsoluteDistanceWithLineOfSight(float modifier)
        {
            _modifier = modifier;
        }
        public float Compute(Vector2 start, Vector2 end, List<Vector2> forbiden, List<Rect> obstacles,
            Dictionary<Vector2, int> redundency)
        {
            float intersectModifier = 0;
            foreach (var obstacle in obstacles)
            {
                bool intersect = IntersectHelpers.LineIntersectsRect(new Point(start.X, start.Y), new Point(end.X, end.Y), obstacle);
                if (intersect)
                    intersectModifier += _modifier;
            }
            return Math.Abs(end.X - start.X) + Math.Abs(end.Y - start.Y) + intersectModifier;
        }
    }
    public class PathFinder
    {
        private static CandidateNode _current;
        private static CandidateNode _startLoc;
        private static Vector2 _targetLoc;
        private static List<CandidateNode> _openList;
        private static List<CandidateNode> _closedList;
        private static int _g;
        private static float _proximityRadius = 20;
        /*private PathRenderer Renderer;
    private List<PathRenderer> pathRenderers;*/
        private List<Vector2> _instanceValidMapNodes;
        private List<Vector2> _forbidenNodes;
        private IAStarHeuristic _heuristic;
        private List<Rect> _obstacles;
        private bool _stopped;
        private Dictionary<Vector2, int> _redundency;

        public void Init(Vector2 start, Vector2 destination, List<Vector2> instanceValidMapNodes, float proximityRadius,
            List<Vector2> forbidenNodes,
            List<Rect> obstacles, IAStarHeuristic heuristic)
        {
            _heuristic = heuristic;
            _obstacles = obstacles;
            _forbidenNodes = forbidenNodes;
            _proximityRadius = proximityRadius;
            _instanceValidMapNodes = instanceValidMapNodes;
            /*pathRenderers?.Clear();
        pathRenderers = new List<PathRenderer>();
        Renderer = new PathRenderer();*/
            _current = null;
            _startLoc = new CandidateNode() { Position = start };
            _targetLoc = destination;
            _openList = new List<CandidateNode>();
            _closedList = new List<CandidateNode>();
            _g = 0;
            _openList.Add(_startLoc);
            _redundency = new Dictionary<Vector2, int>();
        }

        public async Task<List<CandidateNode>> Start(IPathProgressDisplayer display)
        {
            while (_openList.Count > 0 && !_stopped)
            {
                // algorithm's logic goes here
                // get the square with the lowest F score
                var lowest = _openList.Min(l => l.F_Score);
                _current = _openList.First(l => Math.Abs(l.F_Score - lowest) < 0.05);

                // add the current square to the closed list
                _closedList.Add(_current);
                await display.NewClosed(_current);



                // remove it from the open list
                _openList.Remove(_current);

                //if (closedList.FirstOrDefault(l => l.Position.x == targetLoc.x && l.Position.y == targetLoc.y) != null) //Original
                if (_closedList.FirstOrDefault(l => Math.Abs(l.Position.X - _targetLoc.X) < 0.05 && Math.Abs(l.Position.Y - _targetLoc.Y) < 0.05) != null)
                    break;

                var adjacentNodes = GetWalkableAdjacentNodes(_current.Position, _instanceValidMapNodes);
                _g++;
                await display.NewCandidates(adjacentNodes);

                foreach (var adjacentSquare in adjacentNodes)
                {
                    if (_redundency.ContainsKey(adjacentSquare.Position))
                    {
                        _redundency[adjacentSquare.Position] += 1;
                    }
                    else
                    {
                        _redundency.Add(adjacentSquare.Position, 1);
                    }
                    // if this adjacent square is already in the closed list, ignore it
                    var alreadyInClosedList = _closedList.FirstOrDefault(l => l.Position == adjacentSquare.Position);
                    if (alreadyInClosedList != null)
                    {


                        continue;
                    }

                    // if it's not in the open list...
                    if (_openList.FirstOrDefault(l => l.Position == adjacentSquare.Position) == null)
                    {
                        // compute its score, set the parent
                        adjacentSquare.G_Score = _g;
                        adjacentSquare.H_Score = _heuristic.Compute(adjacentSquare.Position, _targetLoc, _forbidenNodes, _obstacles, _redundency);
                        adjacentSquare.F_Score = adjacentSquare.G_Score + adjacentSquare.H_Score;
                        adjacentSquare.Parent = _current;

                        // and add it to the open list
                        _openList.Insert(0, adjacentSquare);
                        /*PathRenderer pathDisplay = new PathRenderer();
                    pathDisplay.DrawGoalLine(current.Position, adjacentSquare.Position);
                    pathRenderers.Add(pathDisplay);*/
                    }
                    else
                    {
                        // test if using the current G score makes the adjacent square's F score
                        // lower, if yes update the parent because it means it's a better path
                        if (_g + adjacentSquare.H_Score < adjacentSquare.F_Score)
                        {
                            adjacentSquare.G_Score = _g;
                            adjacentSquare.F_Score = adjacentSquare.G_Score + adjacentSquare.H_Score;
                            adjacentSquare.Parent = _current;
                        }
                    }
                }

            }

            List<CandidateNode> finalPath = new List<CandidateNode>();
            CandidateNode temp = _closedList[_closedList.IndexOf(_current)];
            if (temp == null) return null;
            do
            {
                finalPath.Add(temp);
                temp = temp.Parent;
            } while (temp != _startLoc && temp != null);
            return finalPath;

            //_closedList.Reverse();
            //return _closedList;
        }
        List<CandidateNode> GetWalkableAdjacentNodes(Vector2 currentLoc, List<Vector2> map)
        {
            //draw proximity area around current position
            /*Bounds proximityBounds = new Bounds(new Vector3(currentLoc.x - _proximityRadius, currentLoc.y - _proximityRadius, 0), new Vector3(_proximityRadius * 2, _proximityRadius * 2, 0));*/

            return
                //get map nodes which falls inside the proximity bounds and are not forbidden nodes (obstacles)
                map
                    .Where(p => !_forbidenNodes.Contains(p))
                    .Where(mapNode => IsInBound(currentLoc, mapNode))
                    //create walkable candidate Node
                    .Select(node => new CandidateNode() { Position = new Vector2(node.X, node.Y) })
                    .ToList();
        }

        bool IsInBound(Vector2 currentPoint, Vector2 targetPoint)
        {
            return IntersectHelpers.IsPointInRect(targetPoint, currentPoint, _proximityRadius * 2, _proximityRadius * 2);
            //float left = currentPoint.X - _proximityRadius;
            //float right = left + _proximityRadius * 2;
            //float top = currentPoint.Y - _proximityRadius;
            //float bottom = top + _proximityRadius * 2;

            //return targetPoint.X >= left
            //       && targetPoint.X <= right
            //       && targetPoint.Y >= top
            //       && targetPoint.Y <= bottom;


        }

        private float ComputeHScore(Vector2 current, Vector2 destination)
        {

            return 0;
        }

        public void Stop()
        {
            _stopped = true;
        }
    }

    public class IntersectHelpers
    {
        public static bool IsPointInRect(Vector2 target, Vector2 rectPosition, float widthRadius, float heightRadius)
        {
            var rect = new Rect(new Point(rectPosition.X - widthRadius, rectPosition.Y - heightRadius), new Size(widthRadius * 2, heightRadius * 2));
            return rect.Contains(target.X, target.Y);
        }
        public static bool LineIntersectsRect(Point p1, Point p2, Rect r)
        {
            return LineIntersectsLine(p1, p2, new Point(r.X, r.Y), new Point(r.X + r.Width, r.Y)) ||
                   LineIntersectsLine(p1, p2, new Point(r.X + r.Width, r.Y), new Point(r.X + r.Width, r.Y + r.Height)) ||
                   LineIntersectsLine(p1, p2, new Point(r.X + r.Width, r.Y + r.Height), new Point(r.X, r.Y + r.Height)) ||
                   LineIntersectsLine(p1, p2, new Point(r.X, r.Y + r.Height), new Point(r.X, r.Y)) ||
                   (r.Contains(p1) && r.Contains(p2));
        }

        private static bool LineIntersectsLine(Point l1p1, Point l1p2, Point l2p1, Point l2p2)
        {
            float q = (float)((l1p1.Y - l2p1.Y) * (l2p2.X - l2p1.X) - (l1p1.X - l2p1.X) * (l2p2.Y - l2p1.Y));
            float d = (float)((l1p2.X - l1p1.X) * (l2p2.Y - l2p1.Y) - (l1p2.Y - l1p1.Y) * (l2p2.X - l2p1.X));

            if (d == 0)
            {
                return false;
            }

            float r = q / d;

            q = (float)((l1p1.Y - l2p1.Y) * (l1p2.X - l1p1.X) - (l1p1.X - l2p1.X) * (l1p2.Y - l1p1.Y));
            float s = q / d;

            if (r < 0 || r > 1 || s < 0 || s > 1)
            {
                return false;
            }

            return true;
        }

    }


}