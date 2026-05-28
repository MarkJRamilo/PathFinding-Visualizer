using System.Runtime.InteropServices;
using Microsoft.VisualBasic;

public class MazeGenerationAlgorithms
{
    Random randomNumber = new Random();
    Node[,] Grid;
    readonly Action stateHasChangedCallback;
    public MazeGenerationAlgorithms(Action stateHasChangedCallback, Node[,] Grid)
    {
        this.stateHasChangedCallback = stateHasChangedCallback;
        this.Grid = Grid;
    }

    

    public async Task run(string mazeAlgirithmToRun) //Grid(x, y)
    {

        switch(mazeAlgirithmToRun){

            case "recursiveDivision":
            clearMaze();
            buildOuterWall(); 
            await recursiveDivision(1 ,1 , Grid.GetLength(0) - 2, Grid.GetLength(1) - 2);
            break;

            case "iterativeBacktracker": 
            clearMaze();
            buildOuterWall(); 
            await iterativeBacktracker();
            break;

            case "kruskalsAlgorithm":
            clearMaze();
            buildOuterWall(); 
            await kruskalsAlgorithm();
            break;

            case "clearWalls": 
            clearMaze();
            break;
        }

        //await recursiveDivision(1 ,1 , Grid.GetLength(0) - 2, Grid.GetLength(1) - 2);
        //await iterativeBacktracker();
       
       //await kruskalsAlgorithm();
       // xMax = Grid.GetLength(0) - 2;
        //yMax = Grid.GetLength(1) - 2;
    }

    private async Task  recursiveDivision(int x = 1, int y = 1, int xMax = 0, int yMax = 0)
    {
        

        bool isHorizontal = ( x - xMax < y - yMax)? true : false; //Horizontal if x axis is bigger

        if (isHorizontal)
        {
            if((xMax - x) < 1) return;  // stop if there is no middle

            int xWallTarget = randomNumber.Next(x, xMax);  //draw | wall at this point 
            if(xWallTarget % 2 != 0) xWallTarget += 1; 

            int yPassageTarget =  randomNumber.Next(y, yMax);
            if(yPassageTarget % 2 == 0) yPassageTarget += 1; 

            await drawVerticalWall(y, yMax, xWallTarget);
            await createPassage(Grid[xWallTarget,yPassageTarget]);

            var task1 = recursiveDivision(x, y, xWallTarget - 1 ,yMax); // first rercursion divides gride into 2, this divides the 1st of that 2
            var task2 = recursiveDivision(xWallTarget + 1, y, xMax, yMax); // this divides the 2nd of that 2

            await Task.WhenAll(task1, task2); //allow algorithm to only leave when all recursions are node
            
        }else
        {
            if((yMax - y) < 1) return; // stop if there is no middle

            int yWallTarget = randomNumber.Next(y, yMax );  //draw <---> wall at this point 
            if(yWallTarget % 2 != 0) yWallTarget += 1; //wall are only on even grid

            int xPassageTarget =  randomNumber.Next(x, xMax ); //create an opening at this point (passages only on odd nums)
            if(xPassageTarget % 2 == 0) xPassageTarget += 1; //Passages are only on odd grid

            await drawHorizontalWall(x, xMax, yWallTarget);
            await createPassage(Grid[xPassageTarget,yWallTarget]);

            var task1 = recursiveDivision(x, y, xMax, yWallTarget - 1); // first rercursion divides gride into 2, this divides the 1st of that 2
            var task2 = recursiveDivision(x, yWallTarget + 1, xMax, yMax); // this divides the 2nd of that 2

            await Task.WhenAll(task1, task2);
        }

    }

    public async Task iterativeBacktracker() //NOTE: This algorithm uses NODE.PARENT to build path between nodes checked (because checking skips by 2 nodes)
    {
        //Set up Stack
        Stack<Node> nodesToVisitForMaze = new Stack<Node>(); 
        List<Node> shuffleThisSetOfNode = new List<Node>();

        //Create Starting Point
        nodesToVisitForMaze.Push(Grid[1,1]);

        //Start building wall
        setWholeGridAsWall();
        stateHasChangedCallback.Invoke();
         await Task.Delay(1000);

        while (nodesToVisitForMaze.Count() != 0)
        {
            //get indexes of current node
            nodesToVisitForMaze.TryPop(out Node? currentNode);
            int nodeRow = currentNode!.RowIndex;
            int nodeCol = currentNode!.ColumnIndex;
            
            if(currentNode.Parent != null) currentNode.Parent.removeWall() ; // Starting point is alwyas null (Parent Node is the middle Node between last and current node)
            await createPassage(currentNode);

            Console.WriteLine("TRIGGERD: " + nodeRow + " AND " + nodeCol);

            //Check Adjacent of current nodes if not out of bounds and place to List to shuffle
                                                //Node to check       //node between node to  check and current node            //add node to stack to be visited
            if(checkNodeIfValidToTurnToPassage(nodeRow - 2, nodeCol, Grid[nodeRow - 1, nodeCol])){shuffleThisSetOfNode.Add(Grid[nodeRow - 2, nodeCol]); Console.WriteLine("TRIGGERD");} //Top
            if(checkNodeIfValidToTurnToPassage(nodeRow + 2, nodeCol, Grid[nodeRow + 1, nodeCol])){shuffleThisSetOfNode.Add(Grid[nodeRow + 2, nodeCol]); Console.WriteLine("TRIGGERD");} //Bottom
            if(checkNodeIfValidToTurnToPassage(nodeRow, nodeCol + 2, Grid[nodeRow, nodeCol + 1])){shuffleThisSetOfNode.Add(Grid[nodeRow, nodeCol + 2]); Console.WriteLine("TRIGGERD");} //Right
            if(checkNodeIfValidToTurnToPassage(nodeRow, nodeCol - 2, Grid[nodeRow, nodeCol - 1])){shuffleThisSetOfNode.Add(Grid[nodeRow, nodeCol - 2]); Console.WriteLine("TRIGGERD");} //Left

            if (shuffleThisSetOfNode.Count() != 0) //Skip delay and statechange when backtracking on Dead Ends
            {
               
                await Task.Delay(20);
            }

            //randomly add nodes from list to nodesToVisitForMaze Stack
            while(shuffleThisSetOfNode.Count() != 0)
            {
                int randomIndexRelativeToListCount = randomNumber.Next(0, shuffleThisSetOfNode.Count()); //get random index
                nodesToVisitForMaze.Push(shuffleThisSetOfNode[randomIndexRelativeToListCount]); //update Stack
                shuffleThisSetOfNode.RemoveAt(randomIndexRelativeToListCount); // remove node from list
            }


        }

    }

    public async Task kruskalsAlgorithm()  //NOTE: This algorithm uses NODE.PARENT of "nodeAdjacentToEdge"s in the same logic of tree data structure (sacrificing this aglorithms code readability for a simpler Node Class)
    {
        List<Node> edgesBetweenVisitableNodes = setWholeGridInCheckeredPatternAndGetEdges();
        Node nodeAdjacentToEdge1; //the EDGE NODE in edgesBetweenVisitableNodes are in between this nodes (either top and bottom OR right and left)
        Node nodeAdjacentToEdge2;
        await Task.Delay(1000);


        while (edgesBetweenVisitableNodes.Count() != 0)
        {
            int randomIndex = randomNumber.Next(0, edgesBetweenVisitableNodes.Count()); //get a random index

            //get coordinate of current node chosen from random index
            int nodeRow = edgesBetweenVisitableNodes[randomIndex].RowIndex;
            int nodeCol = edgesBetweenVisitableNodes[randomIndex].ColumnIndex;

            if (Grid[nodeRow, nodeCol + 1].isWall == true) //If top node is wall, this current node is and edge between right and left node (if not then top and bottom)
            {
                nodeAdjacentToEdge1 = Grid[nodeRow + 1, nodeCol]; // right
                nodeAdjacentToEdge2 = Grid[nodeRow - 1, nodeCol]; // left
            }
            else
            {
                nodeAdjacentToEdge1 = Grid[nodeRow, nodeCol + 1]; // Top
                nodeAdjacentToEdge2 = Grid[nodeRow, nodeCol - 1]; // Bottom
            }

            Node rootParentOfNodeAdjacentToEdge2 =  getNodeParentRoot(nodeAdjacentToEdge2);

            if (getNodeParentRoot(nodeAdjacentToEdge1) != rootParentOfNodeAdjacentToEdge2) //If the 2 nodes are not linked (checked if their parent -> parent.parent etc eventualy lead to the same root)
            {
                rootParentOfNodeAdjacentToEdge2.Parent = nodeAdjacentToEdge1;//link the 2 adjacent nodes together
                await createPassage(Grid[nodeRow, nodeCol]); //open the edge if adjacent nodes are not linked
                await Task.Delay(10);
            }

            edgesBetweenVisitableNodes.RemoveAt(randomIndex);
        }

        
    }

    private Node getNodeParentRoot(Node nodeToGetRootParent)
    {
        while(nodeToGetRootParent.Parent != null)
        {
            nodeToGetRootParent = nodeToGetRootParent.Parent;
        }

        return nodeToGetRootParent;
    }
    private bool checkNodeIfValidToTurnToPassage (int nodeRow, int nodeCol, Node nodeToBecomePathBetweenCurentAndThisNodeChecked){
        if (nodeRow >= 0 && nodeCol >= 0 && nodeRow < Grid.GetLength(0) && nodeCol < Grid.GetLength(1) //Check if out of bounds in Array
            && Grid[nodeRow, nodeCol].isWall == true
            && (nodeRow % 2) == 1 // Walls in Grid are only in odd numbers
            && (nodeCol % 2) == 1)
        {
            Grid[nodeRow, nodeCol].Parent = nodeToBecomePathBetweenCurentAndThisNodeChecked;//Create path in the middle of the 2 nodes
            return true;
        }
        else
        {
            return false;
        }
    }

    private void setWholeGridAsWall()
    {
        foreach(Node n in Grid)
        {
            n.setAsWall();
        }
    
    }

    private void clearMaze()
    {
        foreach(Node n in Grid)
        {
            n.removeWall();
        }
    
    }

    private List<Node> setWholeGridInCheckeredPatternAndGetEdges()
    {
        List<Node> nodesThatAreEdges = new List<Node>(); //These are nodes that are in between Nodes that are visitable (color bisque in the checkered pattern)
        for (int r = 1; r < Grid.GetLength(0) - 1; r++)
        {
            for (int c = 1; c < Grid.GetLength(1) - 1; c++) 
            {
                if(c % 2 == 1) 
                {
                    if(r % 2 == 0)
                    {
                        Grid[r,c].setAsWall();
                        nodesThatAreEdges.Add(Grid[r,c]);
                    }
                    else
                    {
                        Grid[r,c].resetNodeToDefualtValues(); //just for assurance that parent is cleared (important for Kruskas Algorithm)
                    }
                }
                else
                {
                    Grid[r,c].setAsWall();

                    if(r % 2 == 1)
                    {
                        nodesThatAreEdges.Add(Grid[r,c]);
                    }
                }
                
            }
        }

        return nodesThatAreEdges;
    }

    private void buildOuterWall()
    {
        for(int i = 0; i < Grid.GetLength(0); i++ )
        {
            
            Grid[i,Grid.GetLength(1) -1].setAsWall(); //top wall
            Grid[i,0].setAsWall(); //bottom wall
            

            if(i < Grid.GetLength(1))
            {
            Grid[Grid.GetLength(0) - 1,i].setAsWall(); //right
            Grid[0,i].setAsWall(); //left
            }

        }
    }

    private async Task drawHorizontalWall(int xMin, int xMax, int yTarget)
    {
        for (int i = xMin; i <= xMax; i++)
        {
            Grid[i, yTarget].setAsWall();

            await Task.Delay(50);
            stateHasChangedCallback.Invoke();
        }   
    }
    private  async Task drawVerticalWall(int yMin, int yMax, int xTarget)
    {
        for (int i = yMin; i <= yMax; i++)
        {
            Grid[xTarget, i].setAsWall();

            await Task.Delay(50);
            stateHasChangedCallback.Invoke();
        }   
    }

    private async Task createPassage(Node node)
    {
        node.removeWall();
        stateHasChangedCallback.Invoke();
    }

}