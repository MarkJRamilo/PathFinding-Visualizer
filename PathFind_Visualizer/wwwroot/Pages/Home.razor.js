export function testIJSConnection(DotNetPassedMethod){
    let nodeMouseMoveChangeType = "empty";
    let cursorImage = document.createElement('img');
    
    console.log("IJS connection successful!");


    document.addEventListener("mousedown",async function(event){
                                //without await, dotnetmethod returns a promise of the task as "object" instead of string
        nodeMouseMoveChangeType = await DotNetPassedMethod.invokeMethodAsync("MouseDownOnNodeBehavior", parseInt(event.clientX), parseInt(event.clientY));// returns either TransferSpecialNode or NodeToWall
    })

    document.addEventListener("mousemove", function(event){
        console.log("X: " + event.clientX + " /Y: " + event.clientY);
        if(nodeMouseMoveChangeType != "empty"){ //allow only if mousedown
            
            DotNetPassedMethod.invokeMethodAsync("MouseMoveOnNodes", parseInt(event.clientX), parseInt(event.clientY), nodeMouseMoveChangeType );

            
        }
    })

    document.addEventListener("mouseup", function(event){

        nodeMouseMoveChangeType = "empty";

    })
    //add action listners for mouse click
}

export function passViewPortWidthSize(){

    return screen.availWidth;
    
}

export function passViewPortHeightSize(){

    return screen.availHeight;
    
}


