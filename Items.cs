using System.Dynamic;
using System.Runtime.CompilerServices;

public class Node
{
    public int RowPosition { get; set; }
    public int ColPosition { get; set; }
    public int RowIndex{get;set;}
    public int ColumnIndex{get;set;}
    public int Distance { get; set; } = 10000;
    public double FCost {get;set;} = 10000; // only used for A* Search (currentdistance + manhattan distance)
    public Node? Parent;

    // public string Color {get; set;} =  "Bisque"; // Bisque - Default State || LawnGreen - Visited State || gold = BestPath State

    public string State {get; set; } = "nodeDefaultState"; 

    public bool InitialStartPoint { get; set;} = false;
    public bool EndPoint { get; set;} = false;

    public bool isVisited {get; set; }= false; // redundancy with State = nodeVisited needed for multiple endpoints search
    public bool isWall {get; set; } = false; // redundancy with State = nodeWall needed when user drags special nodes to walls
    public bool isProtected {get;set;} = false;// True means this is node is path to endnode - To protect from being converted into Dead End Node for Recursive DFS
    public Node()
    {

    }

    public Node(int rowIndex, int columnIndex,int rowPosition, int columnPosition)
    {
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        RowPosition = rowPosition;
        ColPosition = columnPosition; 
    }
    
    public void setAsVisited(int distance, int nthNumberOfEndNode , Node? cameFromThisNode = null)
    {
        Distance = distance;
        Parent = cameFromThisNode;
        isVisited = true;

        switch (nthNumberOfEndNode)
        {
            case 0 : State = "nodeVisited1";
            break;

            case 1 : State ="nodeVisited2";
            break;

            case 2 : State = "nodeVisited3";
            break;
        }

        
    }
    public void setAsWall()
    {
        //resetNodeToDefualtValues();
        isWall = true; // record node is Wall

        State = (InitialStartPoint || EndPoint)? State : "nodeWall"; //ignore State assignment if node is Special Node
        
    }

 public void setAsProtected()
    {
        isProtected = true;
    }
    public void setAsDeadEnd()
    {
        if(!isProtected) State = "nodeDeadEnd";
    }
    public void setAsNewInitialStartPoint(){

        InitialStartPoint = true;
        Distance = 0;
        Parent = null;

    }

    public void setAsNewEndPoint()
    {    
        EndPoint = true;
    }
    
    public void resetToNormalNodeForEraser()
    {    
        EndPoint = false;
    }
    public void swapNodeFromUserMouseMove(bool thisNodeisMouseMoveDestination , bool sourceNodeIsAStartNode) // when user holds special nodes to other nodes
    {
        if (thisNodeisMouseMoveDestination) //user bring the mouse into this node
        {
            //State = isVisited? "nodeVisited1" : "nodeDefaultState"; //Revert State to visited or normal (when node is wall)

            if(sourceNodeIsAStartNode) //get node State of other node and assign here
            {
                InitialStartPoint = true ;
                State = "nodeDefaultState"; //hide wall
            }
            else
            {
                EndPoint = true;
                State = "nodeDefaultState"; //hide wall
            } 


        }else // this Node is where MouseDown Started and dragged to other node
        {
                InitialStartPoint = false;
                EndPoint = false;
                if(isWall)setAsWall();
        }
    }

    public void removeWall()
    {
            isWall = false;
            State="nodeDefaultState";
    }

    

    public void resetNodeToDefualtValues() //retains wall State
    {
        if(InitialStartPoint == true)
        {
            Distance = 0;
            Parent = null;
            isVisited = false;
            isProtected = false;
            State="nodeDefaultState";
            FCost = 10000;
        }else
        {
            Distance = 10000;
            Parent = null;
            isVisited = false;
            isProtected = false;
            State="nodeDefaultState";
            FCost = 10000;
        }

        if(isWall)setAsWall(); //avoid wall not reverting if user puts InitialStartPoint Node in a Wall
    }
    public void InitialStartNodeIsVisited(int distance)
    {
        Distance = distance;
        isVisited = true;
        State="nodeVisited1";
    }

    public void resetNodeForNewStartPoint()
    {
           isVisited = false; 
           Distance = 10000;
    }

    public void markAsBestPath(int nthNumberOfBestPath)
    {
        switch (nthNumberOfBestPath)
        {
            case 1: State = "nodeBestPath1";
            break;
            case 2: State = "nodeBestPath2";
            break;
            case 3: State = "nodeBestPath3";
            break;
        }
        
    }
}
public class StartMarkCoordinate
{
    public int ColPosition { get; set; }
    public int RowPosition { get; set; }
    
    public void setNewMark(int ColPosition, int RowPosition)
    {
        this.ColPosition = ColPosition;
        this.RowPosition = RowPosition;

    }

}

public class EndMarkCoordinates
{
    public int ColPosition { get; set; }
    public int RowPosition { get; set; }

    public void setNewMark(int ColPosition, int RowPosition)
    {
        this.ColPosition = ColPosition;
        this.RowPosition = RowPosition;

    }
    
}