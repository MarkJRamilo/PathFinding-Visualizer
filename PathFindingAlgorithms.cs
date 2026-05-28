using System.Diagnostics.Metrics;
using System.Net;
using System.Runtime.CompilerServices;

public class PathFindingAlgorithms
{

    Node[,] grid;

    int activeDelay = 150; //Delay for render visibility (active)
    int delayValueFromUser = 150; //value to set to delay if not 0 (from "Speed" button in header)
    int currentDistance = 0;
    int pauseAlgorithmAtThisNodeDistance = 10000; //Algorithm will finish at this distance wihtout reaching endnodes
    Node? stopGetBestPathMethodAtThisNode;
    List<Node> endNodesToVisit = new List<Node>(); 
    int numberOfSuccessfulSearchMade = 0; //for Changing color when looking for new endnode
    int skipAlgorithmToThisDistance = 10000; //Delay is 0 until this distance is reached
    int counterForRecursiveDFS = 3; //will limit to 3 set of recrusions (limited to 3 max endnodes) (will be set to 0 when calling recursive algo)
      List<List<Node>> bestPathFromStartToAllEndNodes = new List<List<Node>>(); //Multiple Best Path when user has many Endnodes as Input
    Stack<Node> nodesTouchedToResetToDefault = new Stack<Node>(); //In Queue to change it Default State
    bool stopBidirectionalSearch = false; //To stop other Search of Bidirectional from getting best path and continuing
    

    //Callbacks run METHOD
    readonly Action activateStateChange;
    readonly Action algorithmCompletesWithoutUserInterupting;

    CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
   

    public PathFindingAlgorithms (Action ActivateStateChange, Action AlgorithmCompletesWithoutUserInterupting,Node[,] grid)
    {   
        //callback to allow algo to show best path with 0 activeDelay when user moves special nodes
        algorithmCompletesWithoutUserInterupting = AlgorithmCompletesWithoutUserInterupting;
        activateStateChange = ActivateStateChange;

        //get table grid
        this.grid = grid;
        //get sized used to compute Node indexes in Array for fast access
    }


    public async Task runSearch(string algorithToRun)
    {  
        await resetPathFindAlgorithm(false);
                //Get Special Nodes (starting and endpoints) (target node index to reduce time complexity ^) used css px size for grid (32) and position in px (always divisible by 32)
            foreach(Node n in grid)
            {
                if(n.InitialStartPoint == true)
                {   
                    n.InitialStartNodeIsVisited(currentDistance);
                    nodesTouchedToResetToDefault.Push(n); //To be Pushed/Enqueed in Search Algorithms (Initial Start Point)
                    

                }else if(n.EndPoint == true){
                    
                    endNodesToVisit.Add(n); //Identify and set End Node

                }
            }

         //Create new token for cancelling processes
        //await Task.Delay(50);
        cancelTokenSource.Dispose();
        cancelTokenSource = new CancellationTokenSource();

        try{
            switch(algorithToRun)
            {
                case "Breadth-First":
                await breathFirstSearch(cancelTokenSource.Token);
                break;

                case "A* Search": 
                await AStarSearch(cancelTokenSource.Token);
                break;

                case "Greedy Best-First":
                await greedyBestFirstSearch(cancelTokenSource.Token);
                break;

                case "Bidirectional":
                await BFSBidirectionalSearch(cancelTokenSource.Token); // endNodesToVisit.Count != 1?
                break;

                case "Recursive Depth-First": 
                counterForRecursiveDFS = 0; //allow another chain of recursions to trigger
                while (endNodesToVisit.Count != 0 && counterForRecursiveDFS < 3) //limited to 3 endnodes
                {
                    counterForRecursiveDFS++;
                    try{
                        
                        await recursiveDepthFirstSearch(nodesTouchedToResetToDefault.Peek().RowIndex, nodesTouchedToResetToDefault.Peek().ColumnIndex , cancelTokenSource.Token);
                    }
                    catch(OperationCanceledException)
                    {
                    }
                    finally
                    {
                        cancelTokenSource.Dispose();
                        cancelTokenSource = new CancellationTokenSource();
                    }
                    
                }
                break;
            }
        } catch(Exception e)
        {
            Console.WriteLine("ERROR: "  + e);
        }
        finally
        {
            activeDelay = 0; //Exception typcally from moving special node that that has modified list / throw from cancell token (ensures instant bestpath)
            activateStateChange.Invoke(); //Redundancy in case activeDelay 0 (wont render until algorithm is finished) shouldn't be needed but still has some issues without it
        }
        //NOTE:y
        //algorithm Continues until distanice reaches pauseAlgorithmAtThisNodeDistance
        //pausAtDistance - changed when pressed "pause" button
    }

    //NOTE:access nodeToVist to its corresponding visitedNode with: visitedNode "index" x 2 = ROW, and visitedNode "index" x 2 + 1 = COL
    //looks through neighbors only instead of whole grid
    //get Endpoint index as [row, col]

    //BUG: Queue empty when placing inside a cross section maze
    private async Task breathFirstSearch(CancellationToken cancelToken)
    {
         Queue<Node> nodesToVisit = new Queue<Node>(); //Node in Queue to check its surroundings

        nodesToVisit.Enqueue(nodesTouchedToResetToDefault.Peek()); //Get Initial Node

         while (nodesToVisit.Count != 0 
        && endNodesToVisit.Count != 0
        && nodesToVisit.Peek().Distance <= pauseAlgorithmAtThisNodeDistance )
        {
            cancelToken.ThrowIfCancellationRequested();//Token to allow stoppage at any point of the algo

            //resume activeDelay and animation after reaching this distance
            if (skipAlgorithmToThisDistance  < nodesToVisit.Peek().Distance)
            {
                activeDelay = delayValueFromUser;
                skipAlgorithmToThisDistance = 10000;
            }

            //GET CURRENT node index in array using column and row postion (ie. index[Row, Col])
            int currentNodeRowIndex = nodesToVisit.Peek().RowIndex;
            int currentNodeColIndex = nodesToVisit.Peek().ColumnIndex;
            currentDistance = nodesToVisit.Peek().Distance;
            nodesToVisit.Dequeue();
            
             //Check top: node[,-1] and if out of bounds
            if (checkNodeIfValidToVisit(currentNodeRowIndex, currentNodeColIndex - 1))
            {
                updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex], grid[currentNodeRowIndex, currentNodeColIndex - 1]);
                nodesToVisit.Enqueue(grid[currentNodeRowIndex, currentNodeColIndex - 1]);
            }

            //Check left: node[-1,] is visited and if out of bounds
            if (checkNodeIfValidToVisit(currentNodeRowIndex - 1, currentNodeColIndex))
            {
                updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex], grid[currentNodeRowIndex - 1, currentNodeColIndex]);
                nodesToVisit.Enqueue(grid[currentNodeRowIndex - 1, currentNodeColIndex]);

            }


            //Check bottom Node: node[,1+] (NOTE: index[Row,Col]) 
            if (checkNodeIfValidToVisit(currentNodeRowIndex, currentNodeColIndex + 1))
            {
                updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex], grid[currentNodeRowIndex, currentNodeColIndex + 1]);
                nodesToVisit.Enqueue(grid[currentNodeRowIndex, currentNodeColIndex + 1]); //Add node to nodesToVisit

            }

            //Check right: node[1+ ,] and if out of bounds
            if (checkNodeIfValidToVisit(currentNodeRowIndex + 1, currentNodeColIndex))
            {
                updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex], grid[currentNodeRowIndex + 1, currentNodeColIndex]);
                nodesToVisit.Enqueue(grid[currentNodeRowIndex + 1, currentNodeColIndex]);
            }
            

            if (currentDistance < nodesToVisit.Peek().Distance //Delay visiting layer to show process to user
            && activeDelay != 0) //if not paused by user
            {
                activateStateChange.Invoke();
                await Task.Delay(activeDelay, cancelToken);

            }


            await checkAllEndNodesAndUpdateBestPathList(nodesToVisit, cancelToken);

        }
    }

    private async Task<bool> recursiveDepthFirstSearch(int currentNodeRowIndex, int currentNodeColIndex, CancellationToken cancelToken, Node? parentNode = null)
    {
        //Console.WriteLine("Leaking?");
        //Console.WriteLine("Distance at: " + currentDistance);
        cancelToken.ThrowIfCancellationRequested();
        bool isThisNodeIsADeadEnd = true; //will create a chain reaction in recursion and turn each node to grey if false

        if (endNodesToVisit.Count == 0 
        || currentDistance >= pauseAlgorithmAtThisNodeDistance)//check if all endNodes is Visited and if Paused by user
        {
            counterForRecursiveDFS = 3; //Will stop while loop triggering recurusion DFS
            cancelAllTokens();
        }

        if (skipAlgorithmToThisDistance < currentDistance)  //resume activeDelay and animation after reaching this distance
        {
            activeDelay = delayValueFromUser;
            skipAlgorithmToThisDistance = 10000;
        }

        updateNodeAsVisited(parentNode!, grid[currentNodeRowIndex, currentNodeColIndex]);//Render as visited
        currentDistance = grid[currentNodeRowIndex, currentNodeColIndex].Distance; //update distance

        if (activeDelay != 0) //Show algorithm process if not activeDelay 0
        {
            activateStateChange.Invoke();
            await Task.Delay(activeDelay / 4, cancelToken);//
        }

        if (grid[currentNodeRowIndex, currentNodeColIndex].EndPoint == true) await isTheirANewEndNodeFoundAndUpdate(cancelToken); //run method if this is a new endnode not yet found (will get path to source node and cancel the token)

        //BRANCH RECURSION - will not run if a new endnode is found
        //Right: node[1+ ,] - check if out of bounds and valid for visit
        if (checkNodeIfValidToVisit(currentNodeRowIndex + 1, currentNodeColIndex))//IMPORTANT: check before entering is a must (Checks for out of bounds)
        {
            //if not connected to an dead end and run recursive
            if (!await recursiveDepthFirstSearch(currentNodeRowIndex + 1, currentNodeColIndex, cancelToken, grid[currentNodeRowIndex, currentNodeColIndex]) //pass this current node as parent node
            && isThisNodeIsADeadEnd == true)
            { isThisNodeIsADeadEnd = false; }
        }

        //Top: node[,-1]
        if (checkNodeIfValidToVisit(currentNodeRowIndex, currentNodeColIndex - 1))
        {
            if (!await recursiveDepthFirstSearch(currentNodeRowIndex, currentNodeColIndex - 1, cancelToken, grid[currentNodeRowIndex, currentNodeColIndex])
            && isThisNodeIsADeadEnd == true)
            { isThisNodeIsADeadEnd = false; }
        }

        //Left: node[-1,]
        if (checkNodeIfValidToVisit(currentNodeRowIndex - 1, currentNodeColIndex))
        {

            if (!await recursiveDepthFirstSearch(currentNodeRowIndex - 1, currentNodeColIndex, cancelToken, grid[currentNodeRowIndex, currentNodeColIndex])
            && isThisNodeIsADeadEnd == true)
            { isThisNodeIsADeadEnd = false; }
        }

        //Bottom: node[,+1]
        if (checkNodeIfValidToVisit(currentNodeRowIndex, currentNodeColIndex + 1))
        {

            if (!await recursiveDepthFirstSearch(currentNodeRowIndex, currentNodeColIndex + 1, cancelToken, grid[currentNodeRowIndex, currentNodeColIndex])
            && isThisNodeIsADeadEnd == true)
            { isThisNodeIsADeadEnd = false; }
        }


        //Reaching this at isThisNodeIsADeadEnd == false means the Node in the end of Call Stack in the recursion chain is a dead-End and this node is surrounded by wall    
        if (isThisNodeIsADeadEnd == true
        && grid[currentNodeRowIndex, currentNodeColIndex].EndPoint == false
        && grid[currentNodeRowIndex, currentNodeColIndex].InitialStartPoint == false)//Stops setting endnodes and startpoint as DeadEnd
        {
            grid[currentNodeRowIndex, currentNodeColIndex].setAsDeadEnd();
        }

        return isThisNodeIsADeadEnd;
    }
    
    public async Task AStarSearch(CancellationToken cancelToken) //COPIED FROM BFS and TWEAKED FOR PRIOTY QUEUE (uses manhattanDistanceHeuristic function)
    {
        PriorityQueue<Node, double> nodesToVisit = new PriorityQueue<Node, double>();
        double FCost; // (current distance + 1) + manhattanDistance Cost (this will be assigned to each of the Node items)
        nodesToVisit.Enqueue(nodesTouchedToResetToDefault.Peek(),0); //Get Initial Node
        nodesToVisit.Peek().FCost = 0; //Set FCost to 0 to prevent backtrack

        while (nodesToVisit.Count != 0 
        && endNodesToVisit.Count != 0
        && nodesToVisit.Peek().Distance <= pauseAlgorithmAtThisNodeDistance )
        {
            cancelToken.ThrowIfCancellationRequested();//Token to allow stoppage at any point of the algo

            //resume activeDelay and animation after reaching this distance
            if (skipAlgorithmToThisDistance  < nodesToVisit.Peek().Distance)
            {
                activeDelay = delayValueFromUser;
                skipAlgorithmToThisDistance = 10000;
            }

            //GET CURRENT node index in array using column and row postion (ie. index[Row, Col])
            int currentNodeRowIndex = nodesToVisit.Peek().RowIndex;
            int currentNodeColIndex = nodesToVisit.Peek().ColumnIndex;
            currentDistance = nodesToVisit.Peek().Distance;
            nodesToVisit.Dequeue();

            //Check top: node[,-1] and if out of bounds
            if (checkNodeForAStar(currentNodeRowIndex, currentNodeColIndex - 1)) //valid if not wall/deadend/out of bounds
            {
                if (currentDistance + 1 < grid[currentNodeRowIndex, currentNodeColIndex - 1].Distance) //Proceed if It new distance of node to visit is cheaper (default unvisited == 10000)
                {
                    FCost = currentDistance + 1 + manhattanDistanceHeuristic(grid[currentNodeRowIndex, currentNodeColIndex - 1]); //
                    updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex], grid[currentNodeRowIndex, currentNodeColIndex - 1], FCost);
                    nodesToVisit.Enqueue(grid[currentNodeRowIndex, currentNodeColIndex - 1], FCost); //get manhattan distance then add to Prioty Queue
                }
            }

            //Check left: node[-1,] is visited and if out of bounds
            if (checkNodeForAStar(currentNodeRowIndex - 1, currentNodeColIndex))
            {
                if (currentDistance + 1 < grid[currentNodeRowIndex - 1, currentNodeColIndex].Distance) //skip if better distance, if not update currently visited node
                {
                    FCost = currentDistance + 1 + manhattanDistanceHeuristic(grid[currentNodeRowIndex - 1, currentNodeColIndex]);
                    updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex], grid[currentNodeRowIndex - 1, currentNodeColIndex], FCost);
                    nodesToVisit.Enqueue(grid[currentNodeRowIndex - 1, currentNodeColIndex], FCost);
                }
            }

            //Check bottom Node: node[,1+] (NOTE: index[Row,Col]) 
            if (checkNodeForAStar(currentNodeRowIndex, currentNodeColIndex + 1))
            {
                if (currentDistance + 1 < grid[currentNodeRowIndex, currentNodeColIndex + 1].Distance)
                {
                    FCost = currentDistance + 1 + manhattanDistanceHeuristic(grid[currentNodeRowIndex, currentNodeColIndex + 1]);
                    updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex], grid[currentNodeRowIndex, currentNodeColIndex + 1], FCost);
                    nodesToVisit.Enqueue(grid[currentNodeRowIndex, currentNodeColIndex + 1], FCost); //Add node to nodesToVisit
                }

            }

            //Check right: node[1+ ,] and if out of bounds
            if (checkNodeForAStar(currentNodeRowIndex + 1, currentNodeColIndex))
            {

                if (currentDistance + 1 < grid[currentNodeRowIndex + 1, currentNodeColIndex].Distance)
                {
                FCost = currentDistance + 1 +manhattanDistanceHeuristic(grid[currentNodeRowIndex + 1, currentNodeColIndex]);
                updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex], grid[currentNodeRowIndex + 1, currentNodeColIndex], FCost);
                nodesToVisit.Enqueue(grid[currentNodeRowIndex + 1, currentNodeColIndex], FCost);
                }
            }

            if (nodesToVisit.Peek().Distance != currentDistance //Delay visiting layer to show process to user
            && activeDelay != 0) //if not paused by user
            {
                if(currentDistance > nodesToVisit.Peek().Distance) continue;//Skip to avoid redundant search
                activateStateChange.Invoke();
                await Task.Delay(activeDelay - 50, cancelToken);

            }

             await checkAllEndNodesAndUpdateBestPathList(nodesToVisit, cancelToken);
            }
    }

        public async Task greedyBestFirstSearch(CancellationToken cancelToken) //COPIED FROM BFS and TWEAKED FOR PRIOTY QUEUE (uses manhattanDistanceHeuristic function)
    {
        PriorityQueue<Node, double> nodesToVisit = new PriorityQueue<Node, double>();
        double HCost; // (current distanc + 1) + manhattanDistance Cost (relying only on manhattanDistance for heuristic removes the "searching" aspect, it goes straight to closest endnode without it)
        nodesToVisit.Enqueue(nodesTouchedToResetToDefault.Peek(),0); //Get Initial Node

        while (nodesToVisit.Count != 0 
        && endNodesToVisit.Count != 0
        && nodesToVisit.Peek().Distance <= pauseAlgorithmAtThisNodeDistance )
        {
            cancelToken.ThrowIfCancellationRequested();//Token to allow stoppage at any point of the algo

            //resume activeDelay and animation after reaching this distance
            if (skipAlgorithmToThisDistance  < nodesToVisit.Peek().Distance)
            {
                activeDelay = delayValueFromUser;
                skipAlgorithmToThisDistance = 10000;
            }

            //GET CURRENT node index in array using column and row postion (ie. index[Row, Col])
            int currentNodeRowIndex = nodesToVisit.Peek().RowIndex;
            int currentNodeColIndex = nodesToVisit.Peek().ColumnIndex;
            currentDistance = nodesToVisit.Peek().Distance;
            nodesToVisit.Dequeue();

            //Check top: node[,-1] and if out of bounds
            if (checkNodeIfValidToVisit(currentNodeRowIndex, currentNodeColIndex - 1))
            {
                updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex], grid[currentNodeRowIndex, currentNodeColIndex - 1]);
                HCost = manhattanDistanceHeuristic(grid[currentNodeRowIndex, currentNodeColIndex - 1]); //
                nodesToVisit.Enqueue(grid[currentNodeRowIndex, currentNodeColIndex - 1], HCost); //get manhattan distance then add to Prioty Queue
            }

            //Check left: node[-1,] is visited and if out of bounds
            if (checkNodeIfValidToVisit(currentNodeRowIndex - 1, currentNodeColIndex))
            {
                updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex], grid[currentNodeRowIndex - 1, currentNodeColIndex]);
                HCost = manhattanDistanceHeuristic(grid[currentNodeRowIndex - 1, currentNodeColIndex]);
                nodesToVisit.Enqueue(grid[currentNodeRowIndex - 1, currentNodeColIndex], HCost);
            }


            //Check bottom Node: node[,1+] (NOTE: index[Row,Col]) 
            if (checkNodeIfValidToVisit(currentNodeRowIndex, currentNodeColIndex + 1))
            {
                updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex], grid[currentNodeRowIndex, currentNodeColIndex + 1]);
                HCost = manhattanDistanceHeuristic(grid[currentNodeRowIndex, currentNodeColIndex + 1]);
                nodesToVisit.Enqueue(grid[currentNodeRowIndex, currentNodeColIndex + 1], HCost); //Add node to nodesToVisit
            }

            //Check right: node[1+ ,] and if out of bounds
            if (checkNodeIfValidToVisit(currentNodeRowIndex + 1, currentNodeColIndex))
            {
                updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex], grid[currentNodeRowIndex + 1, currentNodeColIndex]);
                HCost = manhattanDistanceHeuristic(grid[currentNodeRowIndex + 1, currentNodeColIndex]);
                nodesToVisit.Enqueue(grid[currentNodeRowIndex + 1, currentNodeColIndex], HCost);
            }

            if (nodesToVisit.Peek().Distance != currentDistance //Delay visiting layer to show process to user
            && activeDelay != 0) //if not paused by user
            {
                if(currentDistance > nodesToVisit.Peek().Distance) continue;//Skip to avoid redundant search
                activateStateChange.Invoke();
                await Task.Delay(activeDelay - 50, cancelToken);

            }

             await checkAllEndNodesAndUpdateBestPathList(nodesToVisit, cancelToken);
            }
    }


    private async Task BFSBidirectionalSearch(CancellationToken cancelToken)// instance ID to have distinc visitedMode Color with the same method
    {
        int currentNodeRowIndex;
        int currentNodeColIndex;
        //NOTE: Running 2 BFS aysnchronusly without await is causing inconsistencies even with await Task.Yield at each iteration of each BFS

        //FOR SEARCH 1 in Bidirectional BFS
        Queue<Node> nodesToVisit1 = new Queue<Node>(); //Node in Queue to check its surroundings
        nodesToVisit1.Enqueue(nodesTouchedToResetToDefault.Peek()); //Get Initial Node
        updateNodeAsVisited(nodesTouchedToResetToDefault.Peek(), 0);

        //FOR SEARCH 2 in Bidirectional BFS
        Queue<Node> nodesToVisit2 = new Queue<Node>(); //Node in Queue to check its surroundings
        nodesToVisit2.Enqueue(endNodesToVisit[0]); //Get Initial Node
        updateNodeAsVisited(endNodesToVisit[0], 1);


         while (nodesToVisit1.Count != 0 
        && nodesToVisit2.Count != 0 
        && endNodesToVisit.Count != 0
        && nodesToVisit1.Peek().Distance <= pauseAlgorithmAtThisNodeDistance)
        { 
            //resume activeDelay and animation after reaching this distance
            if (skipAlgorithmToThisDistance  < nodesToVisit1.Peek().Distance)
            {
                activeDelay = delayValueFromUser;
                skipAlgorithmToThisDistance = 10000;
            }
            currentDistance = nodesToVisit1.Peek().Distance;

            //FOR SEARCH 1 in Bidirectional BFS
            while (nodesToVisit1.Peek().Distance == currentDistance)//finish all layer before proceeding
            {
                cancelToken.ThrowIfCancellationRequested();//Token to allow stoppage at any point of the algo
                //GET CURRENT node index in array using column and row postion (ie. index[Row, Col])
                currentNodeRowIndex = nodesToVisit1.Peek().RowIndex;
                currentNodeColIndex = nodesToVisit1.Peek().ColumnIndex;
                nodesToVisit1.Dequeue();
                //Check top: node[,-1] and if out of bounds
                if (await checkNodeForBidirectionalSearch(currentNodeRowIndex, currentNodeColIndex - 1, grid[currentNodeRowIndex, currentNodeColIndex], cancelToken))
                {
                    updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex - 1], 0, grid[currentNodeRowIndex, currentNodeColIndex]);
                    nodesToVisit1.Enqueue(grid[currentNodeRowIndex, currentNodeColIndex - 1]);
                }

                //Check left: node[-1,] is visited and if out of bounds
                if (await checkNodeForBidirectionalSearch(currentNodeRowIndex - 1, currentNodeColIndex, grid[currentNodeRowIndex, currentNodeColIndex], cancelToken))
                {
                    updateNodeAsVisited(grid[currentNodeRowIndex - 1, currentNodeColIndex], 0, grid[currentNodeRowIndex, currentNodeColIndex]);
                    nodesToVisit1.Enqueue(grid[currentNodeRowIndex - 1, currentNodeColIndex]);

                }


                //Check bottom Node: node[,1+] (NOTE: index[Row,Col]) 
                if (await checkNodeForBidirectionalSearch(currentNodeRowIndex, currentNodeColIndex + 1, grid[currentNodeRowIndex, currentNodeColIndex], cancelToken))
                {
                    updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex + 1], 0, grid[currentNodeRowIndex, currentNodeColIndex]);
                    nodesToVisit1.Enqueue(grid[currentNodeRowIndex, currentNodeColIndex + 1]); //Add node to nodesToVisit

                }

                //Check right: node[1+ ,] and if out of bounds
                if ( await checkNodeForBidirectionalSearch(currentNodeRowIndex + 1, currentNodeColIndex, grid[currentNodeRowIndex, currentNodeColIndex], cancelToken))
                {
                    updateNodeAsVisited(grid[currentNodeRowIndex + 1, currentNodeColIndex], 0, grid[currentNodeRowIndex, currentNodeColIndex]);
                    nodesToVisit1.Enqueue(grid[currentNodeRowIndex + 1, currentNodeColIndex]);
                }
            }


            //FOR SEARCH 2 in Bidirectional BFS
            while (nodesToVisit2.Peek().Distance == currentDistance)//finish all layer before proceeding
            {
                cancelToken.ThrowIfCancellationRequested();//Token to allow stoppage at any point of the algo
                //GET CURRENT node index in array using column and row postion (ie. index[Row, Col])
                currentNodeRowIndex = nodesToVisit2.Peek().RowIndex;
                currentNodeColIndex = nodesToVisit2.Peek().ColumnIndex;

                nodesToVisit2.Dequeue();
                
                //Check top: node[,-1] and if out of bounds
                if (await checkNodeForBidirectionalSearch(currentNodeRowIndex, currentNodeColIndex - 1, grid[currentNodeRowIndex, currentNodeColIndex], cancelToken))
                {
                    updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex - 1], 1, grid[currentNodeRowIndex, currentNodeColIndex]);
                    nodesToVisit2.Enqueue(grid[currentNodeRowIndex, currentNodeColIndex - 1]);
                }

                //Check left: node[-1,] is visited and if out of bounds
                if (await checkNodeForBidirectionalSearch(currentNodeRowIndex - 1, currentNodeColIndex, grid[currentNodeRowIndex, currentNodeColIndex], cancelToken))
                {
                    updateNodeAsVisited(grid[currentNodeRowIndex - 1, currentNodeColIndex], 1, grid[currentNodeRowIndex, currentNodeColIndex]);
                    nodesToVisit2.Enqueue(grid[currentNodeRowIndex - 1, currentNodeColIndex]);

                }


                //Check bottom Node: node[,1+] (NOTE: index[Row,Col]) 
                if (await checkNodeForBidirectionalSearch(currentNodeRowIndex, currentNodeColIndex + 1, grid[currentNodeRowIndex, currentNodeColIndex], cancelToken))
                {
                    updateNodeAsVisited(grid[currentNodeRowIndex, currentNodeColIndex + 1], 1, grid[currentNodeRowIndex, currentNodeColIndex]);
                    nodesToVisit2.Enqueue(grid[currentNodeRowIndex, currentNodeColIndex + 1]); //Add node to nodesToVisit

                }

                //Check right: node[1+ ,] and if out of bounds
                if ( await checkNodeForBidirectionalSearch(currentNodeRowIndex + 1, currentNodeColIndex, grid[currentNodeRowIndex, currentNodeColIndex], cancelToken))
                {
                    updateNodeAsVisited(grid[currentNodeRowIndex + 1, currentNodeColIndex], 1, grid[currentNodeRowIndex, currentNodeColIndex]);
                    nodesToVisit2.Enqueue(grid[currentNodeRowIndex + 1, currentNodeColIndex]);
                }
            }
            

            if (activeDelay != 0) //if not paused by user
            {
                activateStateChange.Invoke();
                await Task.Delay(activeDelay,cancelToken);
            }

        }
    }


    private double manhattanDistanceHeuristic(Node source) // For Best First Search
    {
        int shortestDistance = 10000;
        int distance;
        int yDifference;
        int xDifference;

        foreach(Node node in endNodesToVisit)
        {
             // get absolute value distance (no negative number)
            xDifference = Math.Abs(source.RowIndex - node.RowIndex); 
            yDifference = Math.Abs(source.ColumnIndex - node.ColumnIndex);

            distance = xDifference + yDifference;

            if (distance < shortestDistance)
            {
                shortestDistance = distance; //always get the shortest distance from all the endnodesToVisit
            }
        }

        return shortestDistance * 1.1; //make heuristic less certain (removing this would make algo go straigth to the closest endnode)
    }

    private bool checkNodeIfValidToVisit (int nodeRow, int nodeCol)
    {
        if (nodeRow >= 0 && nodeCol >= 0 && nodeRow < grid.GetLength(0) && nodeCol < grid.GetLength(1) //Check if out of bounds in Array
            && grid[nodeRow, nodeCol].isVisited == false
            && grid[nodeRow, nodeCol].State != "nodeWall"
            && grid[nodeRow, nodeCol].State != "nodeDeadEnd")
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool checkNodeForAStar (int nodeRow, int nodeCol) //Visited Nodes will be rechecked fo A*
    {
        if (nodeRow >= 0 && nodeCol >= 0 && nodeRow < grid.GetLength(0) && nodeCol < grid.GetLength(1) //Check if out of bounds in Array
            && grid[nodeRow, nodeCol].State != "nodeWall"
            && grid[nodeRow, nodeCol].State != "nodeDeadEnd")
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private async Task<bool> checkNodeForBidirectionalSearch (int nodeToCheckRow, int nodeToCheckCol, Node sourceNode, CancellationToken cancelToken){
        if (nodeToCheckRow < 0 || nodeToCheckCol < 0 || nodeToCheckRow >= grid.GetLength(0) || nodeToCheckCol >= grid.GetLength(1)) return false;//Check if out of bounds in Array

        if( grid[nodeToCheckRow, nodeToCheckCol].isVisited == false
            && grid[nodeToCheckRow, nodeToCheckCol].State != "nodeWall"
            && grid[nodeToCheckRow, nodeToCheckCol].State != "nodeDeadEnd")
        {
            return true;
        }
        else
        {
            if(grid[nodeToCheckRow, nodeToCheckCol].isVisited == true //TRIGGER PATH FOUND
            && grid[nodeToCheckRow, nodeToCheckCol].State != sourceNode.State
            && stopBidirectionalSearch == false)
            {
                getBestPath(grid[nodeToCheckRow, nodeToCheckCol], true);
                getBestPath(sourceNode, true);
                await showBestPathToAllEndPoints(cancelToken);
                cancelAllTokens();
            } 
            return false;
        }
    }

    private void updateNodeAsVisited(Node? nodeSource, Node nodeDestination)
    {
        nodeDestination.setAsVisited(currentDistance + 1, numberOfSuccessfulSearchMade, nodeSource);
        nodesTouchedToResetToDefault.Push(nodeDestination); // Record nodes to be reset (To avoid foreach all nodes)
    }

    private void updateNodeAsVisited(Node nodeSource, Node nodeDestination, double FCost) //Override for A*
    {
            nodeDestination.setAsVisited(currentDistance + 1, numberOfSuccessfulSearchMade, nodeSource);
            nodeDestination.FCost = FCost; //Update new or better FCost (better if Node is already visited but an alternative lower FCost shows)
            nodesTouchedToResetToDefault.Push(nodeDestination); // Record nodes to be reset (To avoid foreach all nodes)
    }

     private void updateNodeAsVisited( Node nodeDestination, int numberForVisitedModeSwitch, Node? nodeSource = null)//Override for Bidirectional BFS
    {
        nodeDestination.setAsVisited(currentDistance + 1, numberForVisitedModeSwitch, nodeSource);
        nodesTouchedToResetToDefault.Push(nodeDestination); // Record nodes to be reset (To avoid foreach all nodes)
    }

    //if an endNode is reached, make visited endnode a new startpoint 
    // and search for other endnodes
    private async Task checkAllEndNodesAndUpdateBestPathList(Queue<Node> nodesToVisit, CancellationToken cancelToken) // METHOD OVERLOAD (for Breath First Search)
    {
        Stack<Node> tempNodeQueue = new Stack<Node>();
        foreach (Node endNode in endNodesToVisit)
        {   

            //if new EndPoint is found 
            if (endNode.isVisited == true)
            {
                //get the best path to reach this Node
                getBestPath(endNode);

                //reset nodeseToVisit Queue
                while (nodesToVisit.Count != 0) //Leave the last one (EndPoint) if their is more endpoints to find
                {
                    nodesToVisit.Dequeue();
                }

                //make sure be the initial node to search in the next endNode search
                nodesToVisit.Enqueue(endNode);
                updateNodeAsVisited(null,endNode); //set endnode found as visited

                //reset "visited" condition of all past visited nodes to allow new pathFind of other endPoints
                foreach(Node node in nodesTouchedToResetToDefault)
                {
                        node.resetNodeForNewStartPoint(); 
                }

                //next getBestPath will stop here
                stopGetBestPathMethodAtThisNode = endNode;
                numberOfSuccessfulSearchMade++; //change color of next search if their is more
                updateNodeAsVisited(null,endNode); //set endnode found as visited
                
                //Display Best path if last EndNode is Found
                if (endNodesToVisit.Count == 1)
                {
                    await showBestPathToAllEndPoints(cancelToken);
                }
                else
                {
                updateNodeAsVisited(null,endNode); //sync color with next search (avoid leaving last node foud a different color)
                }
                break;
            }
        }
        
        endNodesToVisit.Remove(stopGetBestPathMethodAtThisNode!);
    }

    private async Task checkAllEndNodesAndUpdateBestPathList(PriorityQueue<Node, double> nodesToVisit, CancellationToken cancelToken) // METHOD OVERLOAD (for Best First Search)
    {
        Stack<Node> tempNodeQueue = new Stack<Node>();
        foreach (Node endNode in endNodesToVisit)
        {   

            //if new EndPoint is found 
            if (endNode.isVisited == true)
            {
                //get the best path to reach this Node
                getBestPath(endNode);

                //reset nodeseToVisit Queue
                while (nodesToVisit.Count != 0) //Leave the last one (EndPoint) if their is more endpoints to find
                {
                    nodesToVisit.Dequeue();
                }

                //make sure be the initial node to search in the next endNode search
                nodesToVisit.Enqueue(endNode, 0);
                updateNodeAsVisited(null,endNode); //set endnode found as visited   

                //reset "visited" condition of all past visited nodes to allow new pathFind of other endPoints
                foreach(Node node in nodesTouchedToResetToDefault)
                {
                        node.resetNodeForNewStartPoint(); 
                }

                //next getBestPath will stop here
                stopGetBestPathMethodAtThisNode = endNode;
                numberOfSuccessfulSearchMade++; //change color of next search if their is more
                
                //Display Best path if last EndNode is Found
                if (endNodesToVisit.Count == 1)
                {
                    await showBestPathToAllEndPoints(cancelToken);
                }
                 else               
                {
                    updateNodeAsVisited(null,endNode); //sync color with next search (avoid leaving last node foud a different color)
                }
                break;
            }
        }
        
        endNodesToVisit.Remove(stopGetBestPathMethodAtThisNode);
    }

    private async Task isTheirANewEndNodeFoundAndUpdate( CancellationToken cancelToken)
    {   //NOTE: isTheirANewEndNodeFoundAndUpdate for Recursive DFS only triggers when a endnode is found
        //Unlike BFS where it check every visite"D" node if one of them is an endnode
        //Therefore DFS requires a unique checkAllEndNodesAndUpdateBestPathList method

        foreach (Node endNode in endNodesToVisit)
        {
            //if new EndPoint is found 
            if (endNode.isVisited == true)
            {
                //reset "visited" condition ONLY of all past visited nodes to allow new pathFind of other endPoints
                foreach(Node node in nodesTouchedToResetToDefault)
                {
                    node.resetNodeForNewStartPoint();
                }

                //get the best path to reach this Node
                getBestPath(endNode);

                //next getBestPath will stop here
                stopGetBestPathMethodAtThisNode = endNode;

                //Display Best path if last EndNode is Found
                if (endNodesToVisit.Count == 1)
                {
                    counterForRecursiveDFS = 3; //stop further recursion from happening (DFS is in a while loop)
                    await showBestPathToAllEndPoints(cancelToken);

                }

                currentDistance--; //BAD FIX! - the endnoode will be visited twice since the recursion will run again using that node (redundancy is personally ignored since effect is minimal)
                cancelAllTokens(); //stop all recursive to proceed to next search
                break;
            }
        }
        endNodesToVisit.Remove(stopGetBestPathMethodAtThisNode); // stopGetBestPathMethodAtThisNode is empty or redundant if no new endnode is found
    }

    //to add: must only work if all endnodes are visited
    //NOTE: SOMEHOWLOOPING
    /*Count of bestOath to be inserted: 23
Count of bestOath to be inserted: 42
Count of bestOath to be inserted: 23
Count of bestOath to be inserted: 34
Count of bestOath to be inserted: 23
Count of bestOath to be inserted: 34
Count of bestOath to be inserted: 23*/
    private void getBestPath(Node lastNodeVisited, bool getBestPathForBidirectionalSearch = false)
    {
        //bestPathFromStartToAllEndNodes
        List<Node> bestPathToDestination = new List<Node>();
        int counter = 0;
        Node tempNodeHolder;

        //backtrack through each node using their parent node
        while ( lastNodeVisited != null
        && lastNodeVisited != lastNodeVisited.Parent) //avoid infinite loop (caused me TONS of hard shutdowns and time to find!)
        { 
            counter++;
            
            bestPathToDestination.Add(lastNodeVisited);

            tempNodeHolder = lastNodeVisited;
            lastNodeVisited = lastNodeVisited.Parent!; //get parent of last node

            tempNodeHolder.Parent = null;//make lastnode parent null
            tempNodeHolder.setAsProtected(); // protect from Being converted into dead end by Recursive DFS
        }
        
        //bestPathToDestination.Add(lastNodeVisited); //removing this causes startnode (only) to not be marked (problem: causes small redundancy depending on the number of list in bestPathToDestination)

        //from end node to start to start to end
        if(!getBestPathForBidirectionalSearch) bestPathToDestination.Reverse();
        else
        {
            if(bestPathToDestination[bestPathToDestination.Count - 1] != endNodesToVisit[0]) // If StartingPoint
            {
                bestPathToDestination.Reverse();
                bestPathFromStartToAllEndNodes.Insert(0, bestPathToDestination); //always at start
                return;
            }
        }

        //save this path in order to show later when algo is done
       bestPathFromStartToAllEndNodes.Add(bestPathToDestination);
                    
        
    }
    public async Task showBestPathToAllEndPoints(CancellationToken cancelToken)
    {
        try
        {
            algorithmCompletesWithoutUserInterupting.Invoke(); //allow algo to autocomplete when moving special nodes with 0 activeDelay
            int counter = 0; //for recording the number of best path in list (one path per pair of endnode/startpoint)
            List<List<Node>> copyListToAvoidCollectionModified = bestPathFromStartToAllEndNodes.ToList();        

            foreach(List<Node> nodeList in copyListToAvoidCollectionModified) //make copy with ToList() method to try prevent error from reset (modifiying list while using)
            {
                counter++;

                foreach (Node node in nodeList)
                {
                    cancelToken.ThrowIfCancellationRequested();//Token to allow stoppage at any point of the algo
                    node.markAsBestPath(counter);
                    if(activeDelay != 0)activateStateChange.Invoke();
                    await Task.Delay(activeDelay/2, cancelToken);
                }
            }

            //If algorithm reached all endpoints without user pausing => remove activeDelay
        
            activateStateChange.Invoke();
            activeDelay = 0;//make sure to allways skip animation when this completes
            }
        catch (Exception)
        {
            activeDelay = 0;//make sure to allways skip animation when interrupted
        }
        
    }
    public async Task resetPathFindAlgorithm(bool fromUserInputResetButton) //parameter to create different behavior when currently paused or not
    {   
        cancelAllTokens();
        counterForRecursiveDFS = 3; //To prevent recursive algo from invoking new chain of recursion
        endNodesToVisit.Clear();
        bestPathFromStartToAllEndNodes.Clear();
        stopGetBestPathMethodAtThisNode = null;
        numberOfSuccessfulSearchMade = 0; //set color of searched nodes to default light green
        await Task.Yield();

        //Reset All Nodes
        while(nodesTouchedToResetToDefault.Count != 0)
        {
            nodesTouchedToResetToDefault.Pop().resetNodeToDefualtValues();
        }

        currentDistance = 0;

        if (fromUserInputResetButton == true) 
        {
            //set to default values
            skipAlgorithmToThisDistance = 10000;
            pauseAlgorithmAtThisNodeDistance = 10000;
            activateStateChange.Invoke();
            
            activeDelay = delayValueFromUser;
            
        }
   
    }

    
    public void pauseAlgorithmAndSetDelayZero()
    {
        activeDelay = 0;
        skipAlgorithmToThisDistance = currentDistance;//Distance value to bring back activeDelay from 0 value
        pauseAlgorithmAtThisNodeDistance = currentDistance; //Distance value to stop algorithm at

    }

    public void removePause()
    {   
        pauseAlgorithmAtThisNodeDistance = 10000; //
        if(skipAlgorithmToThisDistance == 10000) activeDelay = delayValueFromUser; //algorithm will set the delay to default
    }

    public void cancelAllTokens()
    {
        cancelTokenSource.Cancel();
    }       

    public void modifyPathfindingDelay(int delayValue)
    {
        delayValueFromUser = delayValue;
    }

    public void removeExcessEndnodesForBidirectional()
    {
        bool endnodeFound = false;
        foreach(Node n in grid)
            {
                //scan until an endnode is found. clear succeeding grid from endnodes
                if(n.EndPoint == true && !endnodeFound) 
                {
                    endnodeFound = true;
                }
                else
                {
                    n.resetToNormalNodeForEraser();
                }
            }
    }

}

