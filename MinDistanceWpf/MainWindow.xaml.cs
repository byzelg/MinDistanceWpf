using MinDistanceWpf.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MinDistanceWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public const double _diameter = 30;
        public const double _edgeLabelSize = 20;

        public const int _fontSize = 12;
        public const int _edgeFontSize = 10;

        public int _count;
        public bool _findMinDistance;

        public List<Node> _cloud;
        public ReachableNodeList _reachableNodes;

        public List<Node> _nodes;
        public List<Edge> _edges;

        public Node _edgeNode1, _edgeNode2;
        public SolidColorBrush _unvisitedBrush, _visitedBrush;
        public bool _isGraphConnected;
        public bool rbBellmanDurumBool = false;


        string[] dizi = new string[50];

        TextBlock txtSatir, txt2;

        public MainWindow()
        {
            InitializeComponent();
            drawingCanvas.SetValue(Canvas.ZIndexProperty, 0);

            _cloud = new List<Node>();
            _reachableNodes = new ReachableNodeList();

            _nodes = new List<Node>();
            _edges = new List<Edge>();

            _count = 1;
            _findMinDistance = false;
            _isGraphConnected = true;

            _unvisitedBrush = new SolidColorBrush(Colors.Black);
            _visitedBrush = new SolidColorBrush(Colors.DarkViolet);

        }


        /// <summary>
        /// The event establishes the all the GUI interactions with the user:
        ///     -the creation of nodes
        ///     -the creation of edges
        ///     -invoke the min distance 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DrawingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                Point clickPoint = e.GetPosition(drawingCanvas);

                if (HasClickedOnNode(clickPoint.X, clickPoint.Y))   //Todo: Tıklanan 1. ve 2. düğümün koordinatları
                {
                    //if (rbBellman.IsChecked == true)
                    //    rbBellmanDurumBool = true;
                    //else
                    //    rbBellmanDurumBool = false;
                    AssignEndNodes(clickPoint.X, clickPoint.Y);
                    if (_edgeNode1 != null && _edgeNode2 != null)
                    {
                        if (_findMinDistance)
                        {
                            statusLabel.Content = "Hesaplanıyor...";
                            if (rbDijkstra.IsChecked == true)
                            {
                                DjikstraMinDistanceTask();
                            }
                            else if (rbBellman.IsChecked == true)
                            {
                                BellmanMinDistanceTask();
                            }
                            else
                            {
                                MessageBoxResult result = MessageBox.Show("Lütfen bir algoritma seçiniz!", "Algoritma Seçimi", MessageBoxButton.YesNo, MessageBoxImage.Error);
                            }

                        }
                        else
                        {
                            //build an edge
                            double distance = GetEdgeDistance();
                            if (distance != 0.0)
                            {
                                if (rbDijkstra.IsChecked == true && distance <= 0)
                                {
                                    MessageBox.Show("Dijkstra algoritmasında kenar uzunluğu negatif olamaz! \nUzaklığı " + distance + " olarak girdiniz!", "Hata", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                }
                                else
                                {
                                    Edge edge = CreateEdge(_edgeNode1, _edgeNode2, distance);
                                    _edges.Add(edge);

                                    _edgeNode1.Edges.Add(edge);
                                    _edgeNode2.Edges.Add(edge);

                                    PaintEdge(edge);
                                    //TODO: RadioButton'lar sonradan değiştirilememeli!!
                                }
                            }
                            ClearEdgeNodes();
                        }
                    }                   
                }
                else
                {
                    if (!OverlapsNode(clickPoint))
                    {
                        Node n = CreateNode(clickPoint);
                        _nodes.Add(n);
                        PaintNode(n);
                        _count++;
                        ClearEdgeNodes();
                    }
                }
            }
        }
        public void BellmanMinDistanceTask()
        {
            Task.Factory.StartNew(() =>
                BellmanFindMinDistancePath(_edgeNode1, _edgeNode2)
            )
            .ContinueWith((task) =>
            {
                if (task.IsFaulted)
                    MessageBox.Show("Bir hata oluştu!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                {
                    PaintMinDistancePath(_edgeNode1, _edgeNode2);
                }

            },
            TaskScheduler.FromCurrentSynchronizationContext());
        }
        public void DjikstraMinDistanceTask()
        {

            Task.Factory.StartNew(() =>
                DijkstraFindMinDistancePath(_edgeNode1, _edgeNode2)
            )
            .ContinueWith((task) =>
            {
                if (task.IsFaulted)
                    MessageBox.Show("Bir hata oluştu!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                {
                    PaintMinDistancePath(_edgeNode1, _edgeNode2);
                }

            },
            TaskScheduler.FromCurrentSynchronizationContext());
        }

        public bool RbBellmanDurum
        {
            get
            {
                if (rbBellman.IsChecked == true)
                    return true;
                else
                    return false;
            }
        }

        public void ClearEdgeNodes()
        {
            _edgeNode1 = _edgeNode2 = null;
        }

        /// <summary>
        /// A method to detect whether the user has clicked on a node
        /// used either for edge creation or for indicating the end-points for which to find
        /// the minimum distance
        /// </summary>
        /// <param name="x">x-coordinate</param>
        /// <param name="y">y-coordinate</param>
        /// <returns>Whether a user is clicked on a existing node</returns>
        public bool HasClickedOnNode(double x, double y)
        {
            bool rez = false;
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].HasPoint(new Point(x, y)))
                {
                    rez = true;
                    break;
                }
            }
            return rez;
        }

        /// <summary>
        /// Get a node at a specific coordinate
        /// </summary>
        /// <param name="x">x-coordinate</param>
        /// <param name="y">y-coordinate</param>
        /// <returns>The node that has been found or null if there is no node at the speicied coordinates</returns>
        public Node GetNodeAt(double x, double y)
        {
            Node rez = null;
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].HasPoint(new Point(x, y)))
                {
                    rez = _nodes[i];
                    break;
                }
            }
            return rez;
        }
        /// <summary>
        /// Upon the creation of a new node,
        /// make sure that it is not overlapping an existing node
        /// </summary>
        /// <param name="p">A x,y point</param>
        /// <returns>Whether there is an overlap with an existing node</returns>
        public bool OverlapsNode(Point p)
        {
            bool rez = false;
            double distance;
            for (int i = 0; i < _nodes.Count; i++)
            {
                distance = GetDistance(p, _nodes[i].Center);
                if (distance < _diameter)
                {
                    rez = true;
                    break;
                }
            }
            return rez;
        }

        /// <summary>
        /// Use an additional dialog window to get the distance
        /// for an edge as specified by the user
        /// </summary>
        /// <returns>The distance value specified by the user</returns>
        public double GetEdgeDistance()
        {
            double distance = 0.0;
            if (rbBellman.IsChecked == true || rbDijkstra.IsChecked == true)
            {
                
                DistanceDialog dd = new DistanceDialog();
                dd.Owner = this;

                dd.ShowDialog();
                distance = dd.Distance;
            }
            else
                MessageBox.Show("Kenar girebilmek için önce bir algoritma seçiniz!", "Hata", MessageBoxButton.OK, MessageBoxImage.Hand);

            return distance;
        }
        /// <summary>
        /// Calculate the Eucledean distance between two points
        /// </summary>
        /// <param name="p1">Point 1</param>
        /// <param name="p2">Point 2</param>
        /// <returns>The distance between the two points</returns>
        public double GetDistance(Point p1, Point p2)
        {
            double xSq = Math.Pow(p1.X - p2.X, 2);
            double ySq = Math.Pow(p1.Y - p2.Y, 2);
            double dist = Math.Sqrt(xSq + ySq);

            return dist;
        }

        public void AssignEndNodes(double x, double y)
        {
            Node currentNode = GetNodeAt(x, y);
            if (currentNode != null)
            {
                if (_edgeNode1 == null)
                {
                    _edgeNode1 = currentNode;
                    statusLabel.Content = currentNode.Label + ". düğümü seçtiniz. Şimdi lütfen başka bir düğüm seçiniz. ";
                }
                else if (_edgeNode2 == null)
                {
                    _edgeNode2 = currentNode;
                    statusLabel.Content = currentNode.Label + ". düğümü seçtiniz. " + _edgeNode1.Label + ". düğüm ile " + _edgeNode2.Label + ". düğüm arası kenar uzunluğunu giriniz. ";
                }
                else
                {
                    if (currentNode != _edgeNode1)
                    {
                        _edgeNode2 = currentNode;
                        statusLabel.Content = "Bir düğüm oluşturmak için ekrana tıklayınız.";
                    }
                }
            }
        }

        /// <summary>
        /// Create a new node using the coordinates specified by a point
        /// </summary>
        /// <param name="p">A Point object that carries the coordinates for Node creation</param>
        /// <returns></returns>
        public Node CreateNode(Point p)
        {
            double nodeCenterX = p.X - _diameter / 2;
            double nodeCenterY = p.Y - _diameter / 2;
            Node newNode = new Node(new Point(nodeCenterX, nodeCenterY), p, _count.ToString(), _diameter);
            return newNode;
        }

        /// <summary>
        /// Paint a single node on the canvas
        /// </summary>
        /// <param name="node">A node object carrying the coordinates</param>
        public void PaintNode(Node node)
        {
            //paint the node
            Ellipse ellipse = new Ellipse();
            if (node.Visited)
                ellipse.Fill = _visitedBrush;
            else
                ellipse.Fill = _unvisitedBrush;

            ellipse.Width = _diameter;
            ellipse.Height = _diameter;

            ellipse.SetValue(Canvas.LeftProperty, node.Location.X);
            ellipse.SetValue(Canvas.TopProperty, node.Location.Y);
            ellipse.SetValue(Canvas.ZIndexProperty, 2);
            //add to the canvas
            drawingCanvas.Children.Add(ellipse);

            //paint the node label 
            TextBlock tb = new TextBlock();
            tb.Text = node.Label;
            tb.Foreground = Brushes.White;
            tb.FontSize = _fontSize;
            tb.TextAlignment = TextAlignment.Center;
            tb.SetValue(Canvas.LeftProperty, node.Center.X - (_fontSize / 4 * node.Label.Length));
            tb.SetValue(Canvas.TopProperty, node.Center.Y - _fontSize / 2);
            tb.SetValue(Canvas.ZIndexProperty, 3);

            //add to canvas on top of the cirle
            drawingCanvas.Children.Add(tb);
        }

        public Edge CreateEdge(Node node1, Node node2, double distance)
        {
            return new Edge(node1, node2, distance);
        }

        public void PaintEdge(Edge edge)
        {
            //draw the edge
            Line line = new Line();
            line.X1 = edge.FirstNode.Center.X;
            line.X2 = edge.SecondNode.Center.X;

            line.Y1 = edge.FirstNode.Center.Y;
            line.Y2 = edge.SecondNode.Center.Y;

            if (edge.Visited)
                line.Stroke = _visitedBrush;
            else
                line.Stroke = _unvisitedBrush;

            line.StrokeThickness = 1;
            line.SetValue(Canvas.ZIndexProperty, 1);
            drawingCanvas.Children.Add(line);

            //draw the distance label
            Point edgeLabelPoint = GetEdgeLabelCoordinate(edge);
            TextBlock tb = new TextBlock();
            tb.Text = edge.Length.ToString();
            tb.Foreground = Brushes.White;

            if (edge.Visited)
                tb.Background = _visitedBrush;
            else
                tb.Background = _unvisitedBrush;

            tb.Padding = new Thickness(5);
            tb.FontSize = _edgeFontSize;

            tb.MinWidth = _edgeLabelSize;
            tb.MinHeight = _edgeLabelSize;

            tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            tb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            tb.TextAlignment = TextAlignment.Center;

            tb.SetValue(Canvas.LeftProperty, edgeLabelPoint.X);
            tb.SetValue(Canvas.TopProperty, edgeLabelPoint.Y);
            tb.SetValue(Canvas.ZIndexProperty, 2);
            drawingCanvas.Children.Add(tb);
        }

        /// <summary>
        /// Calculate the coordinates where an edge label is to be drawn
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public Point GetEdgeLabelCoordinate(Edge edge)
        {

            double x = Math.Abs(edge.FirstNode.Location.X - edge.SecondNode.Location.X) / 2;
            double y = Math.Abs(edge.FirstNode.Location.Y - edge.SecondNode.Location.Y) / 2;

            if (edge.FirstNode.Location.X > edge.SecondNode.Location.X)
                x += edge.SecondNode.Location.X;
            else
                x += edge.FirstNode.Location.X;

            if (edge.FirstNode.Location.Y > edge.SecondNode.Location.Y)
                y += edge.SecondNode.Location.Y;
            else
                y += edge.FirstNode.Location.Y;

            return new Point(x, y);
        }

        public void BellmanFindMinDistancePath(Node start, Node end)
        {
            _cloud.Clear();
            _reachableNodes.Clear();
            Node currentNode = start;
            currentNode.Visited = true;
            start.TotalCost = 0;
            _cloud.Add(currentNode);
            ReachableNode currentReachableNode;
            while (currentNode != end)
            {
                BellmanAddReachableNodes(currentNode);     // Komşu düğümleri kenarlarla bağlar
                BellmanUpdateReachableNodesTotalCost(currentNode);

                //if we cannot reach any other node, the graph is not connected
                if (_reachableNodes.ReachableNodes.Count == 0)
                {
                    _isGraphConnected = false;
                    break;
                }
                int kenarSayisi = _edges.Count;

                //get the closest reachable node
                currentReachableNode = _reachableNodes.ReachableNodes[0];
                //remove if from the reachable nodes list
                _reachableNodes.RemoveReachableNode(currentReachableNode);
                //mark the current node as visited
                currentNode.Visited = true;
                //set the current node to the closest one from the cloud
                currentNode = currentReachableNode.Node;
                //set a pointer to the edge from where we came from
                currentNode.EdgeCameFrom = currentReachableNode.Edge;
                //mark the edge as visited
                currentReachableNode.Edge.Visited = true;

                _cloud.Add(currentNode);
            }
        }

        /// <summary>
        /// The implementation of the Djikstra algorithm
        /// </summary>
        /// <param name="start">Starting Node</param>
        /// <param name="end">Ending Node</param>
        public void DijkstraFindMinDistancePath(Node start, Node end)
        {
            _cloud.Clear();
            _reachableNodes.Clear();
            Node currentNode = start;
            currentNode.Visited = true;
            start.TotalCost = 0;
            _cloud.Add(currentNode);
            ReachableNode currentReachableNode;

           /* Application.Current.Dispatcher.Invoke((Action)delegate
            {
                RowDefinition rowDef1 = new RowDefinition();
                // TODO: s1s0 start node yazma
                txt2 = new TextBlock();
                txt2.Text = start.Label;
                txt2.FontSize = 12;
                txt2.TextAlignment = TextAlignment.Center;
                Grid.SetRow(txt2, 1);
                Grid.SetColumn(txt2, 0);

                myGrid.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                           (ThreadStart)delegate ()
                           {
                               myGrid.RowDefinitions.Add(rowDef1);
                               myGrid.Children.Add(txt2);
                           });
                Thread.Sleep(100);
            });*/
            while (currentNode != end)
            {
                DijkstraAddReachableNodes(currentNode);     // Komşu düğümleri kenarlarla bağlar
                UpdateReachableNodesTotalCost(currentNode);

                //if we cannot reach any other node, the graph is not connected
                if (_reachableNodes.ReachableNodes.Count == 0)
                {
                    _isGraphConnected = false;
                    break;
                }
                int kenarSayisi = _edges.Count;

                //get the closest reachable node
                currentReachableNode = _reachableNodes.ReachableNodes[0];
                //remove if from the reachable nodes list
                _reachableNodes.RemoveReachableNode(currentReachableNode);
                //mark the current node as visited
                currentNode.Visited = true;
                //set the current node to the closest one from the cloud
                currentNode = currentReachableNode.Node;
                //set a pointer to the edge from where we came from
                currentNode.EdgeCameFrom = currentReachableNode.Edge;
                //mark the edge as visited
                currentReachableNode.Edge.Visited = true;

                //// TODO: benden
                //Application.Current.Dispatcher.Invoke((Action)delegate
                //{
                //    // TODO: s1s0 start node yazma
                //    txt2 = new TextBlock();
                //    txt2.Text = currentNode.Label;
                //    txt2.FontSize = 12;
                //    txt2.TextAlignment = TextAlignment.Center;
                //    Grid.SetRow(txt2, kenarSayisi);
                //    Grid.SetColumn(txt2, 0);

                //    myGrid.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                //        (ThreadStart)delegate ()
                //        {
                //            myGrid.Children.Add(txt2);

                //        });
                //    Thread.Sleep(100);
                //});

                _cloud.Add(currentNode);
            }

            //// Define the Columns
            //ColumnDefinition colDef1 = new ColumnDefinition();
            //ColumnDefinition colDef2 = new ColumnDefinition();
            //ColumnDefinition colDef3 = new ColumnDefinition();
            //myGrid.ColumnDefinitions.Add(colDef1);
            //myGrid.ColumnDefinitions.Add(colDef2);
            //myGrid.ColumnDefinitions.Add(colDef3);

            //// Define the Rows
            //RowDefinition rowDef1 = new RowDefinition();
            //RowDefinition rowDef2 = new RowDefinition();
            //RowDefinition rowDef3 = new RowDefinition();
            //RowDefinition rowDef4 = new RowDefinition();
            //myGrid.RowDefinitions.Add(rowDef1);
            //myGrid.RowDefinitions.Add(rowDef2);
            //myGrid.RowDefinitions.Add(rowDef3);
            //myGrid.RowDefinitions.Add(rowDef4);


        }

        /// <summary>
        /// The method paints the the minimum distance path starting from the end node
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void PaintMinDistancePath(Node start, Node end)
        {
            if (_isGraphConnected)  // TODO: breakpoint
            {
                Node currentNode = end;
                double totalCost = 0;
                while (currentNode != start)
                {
                    currentNode.Visited = true;
                    currentNode.EdgeCameFrom.Visited = true;
                    totalCost += currentNode.EdgeCameFrom.Length;

                    PaintNode(currentNode);
                    PaintEdge(currentNode.EdgeCameFrom);

                    currentNode = GetNeighbour(currentNode, currentNode.EdgeCameFrom);
                }
                //paint the current node -> this is the start node
                if (currentNode != null)
                    PaintNode(currentNode);

                start.Visited = true;
                statusLabel.Content = "Yol Maliyeti: " + totalCost.ToString();
            }
            else
            {
                ClearEdgeNodes();
                _isGraphConnected = true;
                _findMinDistance = false;
                statusLabel.Content = "Bir düğüm oluşturmak için ekrana tıklayınız.";
                MessageBox.Show("Grafik bağlı değil. Bir yol bulunamıyor!", "Hata", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
        }

        public void BellmanAddReachableNodes(Node node)
        {
            Node neighbour;
            ReachableNode rn;

            foreach (Edge edge in node.Edges)
            {
                neighbour = GetNeighbour(node, edge);
                //make sure we don't add the node we came from
                if (node.EdgeCameFrom == null || neighbour != GetNeighbour(node, node.EdgeCameFrom))
                {
                    //make sure we don't add a node already in the cloud
                    //zaten bulutta olan bir düğümü eklememek için
                    if (!_cloud.Contains(neighbour))
                    {
                        //if the node is already reachable
                        //erişilebilir düğümler içinde komşu varsa
                        if (_reachableNodes.HasNode(neighbour))
                        {
                            //if the distance from this edge is smaller than the current total cost
                            //amend the reachable node using the current edge
                            if (node.TotalCost + edge.Length < neighbour.TotalCost)
                            {
                                rn = _reachableNodes.GetReachableNodeFromNode(neighbour);
                                rn.Edge = edge;
                            }
                        }
                        else
                        {
                            rn = new ReachableNode(neighbour, edge);
                            _reachableNodes.AddReachableNode(rn);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// On each iteration we need to discover the reachable nodes by going through all the edges that are incident on the node
        /// </summary>
        /// <param name="node"></param>
        public void DijkstraAddReachableNodes(Node node)
        {
            Node neighbour;
            ReachableNode rn;

            foreach (Edge edge in node.Edges)
            {
                neighbour = GetNeighbour(node, edge);       // TODO: Komşuyu veriyor = neighbour
                                                            // TODO: benden
                                                            //Application.Current.Dispatcher.Invoke((Action)delegate
                                                            //{
                                                            //    // TODO: s1s0 start node yazma
                                                            //    txt2 = new TextBlock();
                                                            //    txt2.Text = node.Label;
                                                            //    txt2.FontSize = 12;
                                                            //    txt2.TextAlignment = TextAlignment.Center;
                                                            //    Grid.SetRow(txt2, 1);
                                                            //    Grid.SetColumn(txt2, 0);

                //    myGrid.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                //        (ThreadStart)delegate ()
                //        {
                //            myGrid.Children.Add(txt2);

                //        });
                //    Thread.Sleep(100);
                //});
                //buraya
               /* Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    //TODO: ADD (kenar,...)
                    txtSatir = new TextBlock();
                    txtSatir.Text = neighbour.Edges[0].Length + "," + txt2.Text;
                    txtSatir.FontSize = 12;
                    txtSatir.TextAlignment = TextAlignment.Center;
                    for (int j = 1; j < _nodes.Count; j++)
                    {//TODO: sıkıntı burada kayma var!
                        if (neighbour.Label != txt2.Text && neighbour.Label == dizi[j]) //kayma komşular
                        {
                            Console.WriteLine(j + ". Komşu: \n" + txtSatir.Text);
                            Grid.SetColumn(txtSatir, j);
                        }
                    }

                    Grid.SetRow(txtSatir, 1);

                    myGrid.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        (ThreadStart)delegate ()
                        {
                            myGrid.Children.Add(txtSatir);

                        });
                    Thread.Sleep(100);
                });*/
                //make sure we don't add the node we came from
                //geldiğimiz düğümü eklememek için
                if (node.EdgeCameFrom == null || neighbour != GetNeighbour(node, node.EdgeCameFrom))
                {
                    //make sure we don't add a node already in the cloud
                    //zaten bulutta bulunan bir düğümü eklememek için
                    if (!_cloud.Contains(neighbour))
                    {
                        //if the node is already reachable
                        //düğüm ulaşılabilirse
                        if (_reachableNodes.HasNode(neighbour))
                        {
                            //if the distance from this edge is smaller than the current total cost
                            //amend the reachable node using the current edge

                            //bu kenardaki mesafe mevcut toplam maliyetten küçükse, 
                            //geçerli kenar kullanılarak ulaşılabilir düğümü değiştirilir
                            if (node.TotalCost + edge.Length < neighbour.TotalCost)
                            {
                                rn = _reachableNodes.GetReachableNodeFromNode(neighbour);
                                rn.Edge = edge;
                            }
                        }
                        else
                        {
                            rn = new ReachableNode(neighbour, edge);
                            _reachableNodes.AddReachableNode(rn);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// On each iteration the total cost of all reachable nodes needs to be recalculated
        /// </summary>
        /// <param name="node">The current node</param>
        public void UpdateReachableNodesTotalCost(Node node)
        {
            double currentCost = node.TotalCost;
            foreach (ReachableNode rn in _reachableNodes.ReachableNodes)
            {
                if (currentCost + rn.Edge.Length < rn.Node.TotalCost || rn.Node.TotalCost == -1)
                    rn.Node.TotalCost = currentCost + rn.Edge.Length;
            }

            _reachableNodes.SortReachableNodes();
        }
        public void BellmanUpdateReachableNodesTotalCost(Node node)
        {
            double currentCost = node.TotalCost;
            foreach (ReachableNode rn in _reachableNodes.ReachableNodes)
            {
                if (currentCost + rn.Edge.Length < rn.Node.TotalCost || rn.Node.TotalCost == -1)
                    rn.Node.TotalCost = currentCost + rn.Edge.Length;
            }

            _reachableNodes.SortReachableNodes();
        }

        /// <summary>
        /// Get the node on the other side of the edge
        /// </summary>
        /// <param name="node">The node from which we look for the neighbour</param>
        /// <param name="edge">The edge that we are currently on</param>
        /// <returns></returns>
        public Node GetNeighbour(Node node, Edge edge) //TODO: Kenarı Veriyor = edge
        {
            if (edge.FirstNode == node)
                return edge.SecondNode;
            else
                return edge.FirstNode;
        }

        public void FindMinDistanceBtn_Click(object sender, RoutedEventArgs e)
        {
            this._findMinDistance = true;
            statusLabel.Content = "Minimum mesafe yolunu bulmak istediğiniz iki düğümü seçiniz.";
        }

        public void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            Clear();
            PaintAllNodes();
            PaintAllEdges();
        }

        public void Clear()
        {
            //this._findMinDistance = false;
            //myGrid.ColumnDefinitions.Clear();
            //myGrid.RowDefinitions.Clear();
            //var textboxes = myGrid.Children.OfType<TextBlock>();
            //foreach (var textBox in textboxes)
            //    textBox.Text = String.Empty;

            //statusLabel.Content = "";

            myGrid.ColumnDefinitions.Clear();
            myGrid.RowDefinitions.Clear();
            var textboxes = myGrid.Children.OfType<TextBlock>();
            foreach (var textBox in textboxes)
                textBox.Text = String.Empty;
            this._cloud.Clear();
            
            this._findMinDistance = false;
            statusLabel.Content = "";

            //en kısa yolu (mor renkli) temizliyor 
            foreach (Node n in _nodes)
                n.Visited = false;

            foreach (Edge e in _edges)
                e.Visited = false;
        }

        public void Restart()
        {
            myGrid.ColumnDefinitions.Clear();
            myGrid.RowDefinitions.Clear();
            var textboxes = myGrid.Children.OfType<TextBlock>();
            foreach (var textBox in textboxes)
                textBox.Text = String.Empty;
            this._nodes.Clear();
            this._edges.Clear();
            this._cloud.Clear();
            this._reachableNodes.Clear();
            this._findMinDistance = false;
            this._count = 1;
            statusLabel.Content = "";
        }

        public void PaintAllNodes()
        {
            foreach (Node n in _nodes)
                PaintNode(n);
        }

        public void PaintAllEdges()
        {
            foreach (Edge e in _edges)
                PaintEdge(e);
        }

        public void RestartBtn_Click(object sender, RoutedEventArgs e)
        {
            Restart();
            drawingCanvas.Children.Clear();
        }
    }
}
