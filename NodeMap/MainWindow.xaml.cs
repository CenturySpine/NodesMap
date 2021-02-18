using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NodeMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Vector2 start;
        private Vector2 end;
        private List<Vector2> forbidenNodes = new List<Vector2>();
        private List<Vector2> map;
        private Point currentPointer;
        private Point endPointer;

        public MainWindow()
        {
            InitializeComponent();
            NodesMap.MouseLeftButtonDown += NodesMap_MouseLeftButtonDown;
            NodesMap.MouseLeftButtonUp += NodesMap_MouseLeftButtonUp;
            forbidenNodes = new List<Vector2>();
        }

        private List<Rect> obstacles = new List<Rect>();



        private void NodesMap_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                endPointer = Mouse.GetPosition(NodesMap);


                obstacles.Add(new Rect(currentPointer, endPointer));
                var newObstacles = map.Where(v =>
                  {

                      return v.X > currentPointer.X && v.X < endPointer.X
                                                    && v.Y > currentPointer.Y && v.Y < endPointer.Y;


                  }).ToList();

                foreach (var obstacle in newObstacles)
                {
                    var visual = NodesMap.Children.OfType<Ellipse>().FirstOrDefault(el => el.Tag is Vector2 v && v == obstacle);
                    if (visual != null)
                    {
                        visual.Fill = new SolidColorBrush(Colors.Red);
                    }
                }
                forbidenNodes.AddRange(newObstacles);

            }

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                var startClick = Mouse.GetPosition(NodesMap);
                inputStartX.Text = startClick.X.ToString();
                inputStartY.Text = startClick.Y.ToString();
                start = new Vector2((float)startClick.X, (float)startClick.Y);

            }

            if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                var endClick = Mouse.GetPosition(NodesMap);
                inputEndX.Text = endClick.X.ToString();
                inputEndY.Text = endClick.Y.ToString();

                end = new Vector2((float)endClick.X, (float)endClick.Y);

            }
        }

        private void NodesMap_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                currentPointer = Mouse.GetPosition(NodesMap);
            }

        }

        private void El_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            FrameworkElement fe = (FrameworkElement)sender;
            nodeDisplay.Text = new Point(Canvas.GetLeft(fe) + fe.Width / 2, Canvas.GetTop(fe) + fe.Height / 2).ToString();
        }

        private void RangeBase_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //if (NodesMap != null)
            //{
            //    NodesMap.RenderTransformOrigin = new Point(0.5, 0.5);
            //    NodesMap.RenderTransform = new ScaleTransform(zoom.Value, zoom.Value);
            //}
        }

        private void Generate_OnClick(object sender, RoutedEventArgs e)
        {
            obstacles.Clear();
            forbidenNodes.Clear();
            NodesMap.Children.Clear();

            start = new Vector2((float)Convert.ToDouble(inputStartX.Text), (float)Convert.ToDouble(inputStartY.Text));
            end = new Vector2((float)Convert.ToDouble(inputEndX.Text), (float)Convert.ToDouble(inputEndY.Text)); ;

            map = new List<Vector2>();
            float ndSpaceing = (float)Convert.ToDouble(inputNodeSpacing.Text);

            float offset = (checkOffsetNodes.IsChecked.HasValue && checkOffsetNodes.IsChecked.Value ? ndSpaceing / 2 : 0.0f);

            for (float i = 0; i < NodesMap.ActualWidth; i += ndSpaceing)
            {
                bool applyOffset = false;
                for (float j = 0; j < NodesMap.ActualHeight; j += ndSpaceing)
                {
                    Vector2 node;
                    if (!applyOffset)
                    {
                        node = new Vector2(i, j);
                        applyOffset = true;
                    }
                    else
                    {
                        node = new Vector2(i + offset, j);
                        applyOffset = false;
                    }
                    map.Add(node);
                }
            }

            bool startAlreadyInMap = false;
            bool endAlreadyInMap = false;
            foreach (var vector2 in map)
            {
                Ellipse el = new Ellipse() { Height = 5, Width = 5, Tag = vector2 };
                if (vector2 == start)
                {
                    startAlreadyInMap = true;
                    el.Fill = new SolidColorBrush(Colors.Green);
                }
                else if (vector2 == end)
                {
                    endAlreadyInMap = true;
                    el.Fill = new SolidColorBrush(Colors.Yellow);
                }

                else if (forbidenNodes.Contains(vector2))
                {
                    el.Fill = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    el.Fill = new SolidColorBrush(Colors.DarkGray);
                }


                NodesMap.Children.Add(el);





                Canvas.SetTop(el, vector2.Y - el.Height / 2);
                Canvas.SetLeft(el, vector2.X - el.Width / 2);

                el.MouseEnter += El_MouseEnter;
            }

            if (!startAlreadyInMap)
            {
                map.Add(start);
                Ellipse el = new Ellipse() { Height = 5, Width = 5, Fill = new SolidColorBrush(Colors.Green), Tag = start };
                NodesMap.Children.Add(el);
                Canvas.SetTop(el, start.Y - el.Height / 2);
                Canvas.SetLeft(el, start.X - el.Width / 2);
            }

            if (!endAlreadyInMap)
            {
                map.Add(end);
                Ellipse el = new Ellipse() { Height = 5, Width = 5, Fill = new SolidColorBrush(Colors.DarkRed), Tag = end };
                NodesMap.Children.Add(el);
                Canvas.SetTop(el, end.Y - el.Height / 2);
                Canvas.SetLeft(el, end.X - el.Width / 2);
            }
        }

        class CanvasProgressDisplay : IPathProgressDisplayer
        {
            private readonly Canvas _nodeContainer;
            private Ellipse _previousCurrent;
            private List<Ellipse> _previousCandidates;

            public CanvasProgressDisplay(Canvas nodeContainer)
            {
                _nodeContainer = nodeContainer;
            }
            public async Task NewClosed(CandidateNode current)
            {

                await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                 {
                     //if (_previousCurrent != null)
                     //{
                     //    _previousCurrent.Fill = new SolidColorBrush(Colors.DarkGray);
                     //}

                     var visual = _nodeContainer.Children.OfType<Ellipse>().FirstOrDefault(el => el.Tag is Vector2 v && v == current.Position);
                     if (visual != null)
                     {
                         visual.Fill = new SolidColorBrush(Colors.Purple);
                         _previousCurrent = visual;

                     }
                     Thread.Sleep(50);
                 }));
            }

            public async Task NewCandidates(List<CandidateNode> adjacentNodes)
            {
                await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                  {
                      if (_previousCandidates == null)
                          _previousCandidates = new List<Ellipse>();

                      //if (_previousCandidates != null && _previousCandidates.Any())
                      //{
                      //    foreach (var previousCandidate in _previousCandidates)
                      //    {
                      //        previousCandidate.Fill = new SolidColorBrush(Colors.DarkGray);
                      //    }
                      //    _previousCandidates.Clear();
                      //}
                      foreach (var adjnode in adjacentNodes)
                      {
                          var visual = _nodeContainer.Children.OfType<Ellipse>().FirstOrDefault(el => el.Tag is Vector2 v && v == adjnode.Position);
                          if (visual != null)
                          {
                              visual.Fill = new SolidColorBrush(Colors.Lime);
                              _previousCandidates.Add(visual);

                          }
                      }
                      Thread.Sleep(50);

                  }));
            }
        }
        private async void FindPath_OnClick(object sender, RoutedEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            _pf = new PathFinder();
            var proxRad = (float)Convert.ToDouble(inputProximity.Text);

            //Shuffle(map);
            ClearNodes();

            IAStarHeuristic heur = null;
            if (h_no.IsChecked.HasValue && h_no.IsChecked.Value)
            {
                heur = new NoHeuristic();
            }
            if (h_ad.IsChecked.HasValue && h_ad.IsChecked.Value)
            {
                heur = new CurrentToEndAbsoluteDistance();
            }
            if (h_adls.IsChecked.HasValue && h_adls.IsChecked.Value)
            {
                heur = new CurrentToEndAbsoluteDistanceWithLineOfSight((float) Convert.ToDouble(obstModifierInput.Text));
            }
            if (h_adlsc.IsChecked.HasValue && h_adlsc.IsChecked.Value)
            {
                heur = new CurrentToEndAbsoluteDistanceWithRedundency((float)Convert.ToDouble(obstModifierInput.Text));
            }

            var finalPath = await Task.Run(async () =>
            {
                sw.Reset();
                _pf.Init(start, end, map, proxRad, forbidenNodes, obstacles, heur);
                sw.Start();
                var result = await _pf.Start(new CanvasProgressDisplay(NodesMap));
                sw.Stop();
                return result;
            });
            displayfindPathTime.Text = sw.Elapsed.TotalMilliseconds.ToString() + "ms";

            sw.Reset();
            sw.Start();
            List<Tuple<Vector2, Vector2>> meshes = new List<Tuple<Vector2, Vector2>>();
            for (int i = 0; i < finalPath.Count; i++)
            {
                if (i + 1 < finalPath.Count)
                {
                    meshes.Add(new Tuple<Vector2, Vector2>(finalPath[i].Position, finalPath[i + 1].Position));
                }
            }

            foreach (var tuple in meshes)
            {
                Line line = new Line
                {
                    X1 = tuple.Item1.X,
                    X2 = tuple.Item2.X,
                    Y1 = tuple.Item1.Y,
                    Y2 = tuple.Item2.Y,
                    Stroke = new SolidColorBrush(Colors.CornflowerBlue),
                    StrokeThickness = 1
                };
                NodesMap.Children.Add(line);
            }
            sw.Stop();
            Console.WriteLine("Drawing meshes time : " + sw.Elapsed.TotalMilliseconds + "ms");

        }

        private void ClearNodes()
        {
            foreach (var node in NodesMap.Children.OfType<Ellipse>())
            {
                if (node.Tag is Vector2 v1 && v1 == start)
                    continue;
                if (node.Tag is Vector2 v2 && v2 == end)
                    continue;
                if (node.Tag is Vector2 v3 && forbidenNodes.Contains(v3))
                    continue;

                node.Fill = new SolidColorBrush(Colors.DarkGray);
            }
        }

        private static Random rng = new Random();
        private PathFinder _pf;

        public static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private void StopPath_OnClick(object sender, RoutedEventArgs e)
        {
            _pf.Stop();
        }
    }

    public interface IPathProgressDisplayer
    {
        Task NewClosed(CandidateNode current);
        Task NewCandidates(List<CandidateNode> adjacentNodes);
    }
}
