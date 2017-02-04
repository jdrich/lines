"use strict";

var host = window.location.host + ':14150';

var mouseDown = false;
var mouseWasDown = true;

var offsetX = 0;
var offsetY = 0;

var mousePos = [0.0, 0.0];

var color;

var socket;
var socketOpen = false;

var drawGrid = false;

var room;

var player;

// color -> segment -> point
var players = {};

var drawHud = function (context) {
    var canvas = context.canvas;

    context.fillStyle = "#000000";
    context.font = "10px Courier New";
    
    // Draw offset
    context.fillText(offsetX + " x " + offsetY, 10, 20);

    // Draw color
    context.fillStyle = color;
    context.fillText(color, 10, canvas.height - 20);

    // Draw connection
    context.fillStyle = socketOpen ? '#00ff00' : '#ff0000';
    context.fillText(socketOpen ? ":)" : ":(", canvas.width - 20, canvas.height - 20);

    // Draw room 
    if(typeof room != "undefined") {
        context.fillStyle = "#000000";
        context.fillText(room, canvas.width - 20, 20);
    }

    // Draw box
    context.beginPath();
    context.strokeStyle = '#000000';
    context.rect(0, 0, canvas.width - context.lineWidth, canvas.height - context.lineWidth);
    context.stroke();
};

var getContext = function (canvas) {
    var context = canvas.getContext('2d');

    context.translate(0.5, 0.5);

    return context;
};

var getColor = function () {
    return '#' + getHex(6);
};

var getHex = function (length) { 
    var hex = '';

    while (length > 0) {
        hex += (Math.floor(Math.random() * 16)).toString(16);

        length--;
    }

    return hex;
}

var moveTo = function(room) {
    socket.send(player + ">move>" + room);
};

var clear = function () {
    socket.send(player + ">clear>");

    clearLocal();
}

var clearLocal = function () {
    for (var player in players) {
        players[player] = [];
    }
}

var attachKeyListener = function () {
    var keyups = {
        // space
        32: function () { clear(); },
        // g
        71: function () { drawGrid = !drawGrid; },
        // c
        67: function () { document.getElementById('color').click(); }
    };

    var keypresses = {
        // w
        87: function () { offsetY > 0 && offsetY--; },
        // a
        65: function () { offsetX > 0 && offsetX--; },
        // s
        83: function () { offsetY++; },
        // d
        68: function () { offsetX++; },
        // w
        119: function () { offsetY > 0 && offsetY--; },
        // a
        97: function () { offsetX > 0 && offsetX--; },
        // s
        115: function () { offsetY++; },
        // d
        100: function () { offsetX++; },
    };

    document.body.onkeypress = function (e) {
        
        var keyCode = e.keyCode || e.which;
        
        if (typeof keypresses[keyCode] != "undefined") {
            keypresses[keyCode]();
            keypresses[keyCode]();
        }
    }

    document.body.onkeyup = function (e) {
        if (typeof keyups[e.keyCode] != "undefined") {
            keyups[e.keyCode]();
        }
        
        if(e.keyCode >= 48 && e.keyCode <= 57) {
            moveTo(e.keyCode - 48);
        }
    }
};

var changeColor = function (newColor) {
    color = newColor;

    if (typeof players[color] == "undefined") {
        players[color] = [];
    }
};

var attachColorChangeListener = function () {
    document.getElementById('color').onchange = function () {
        changeColor(document.getElementById('color').value);
    }
}

var drawGridLines = function (context) {
    var lineX = 50 - offsetX;
    var lineY = 50 - offsetY;

    context.strokeStyle = '#dddddd'

    while (lineX < context.canvas.width) {
        context.beginPath();
        context.moveTo(lineX, 0);
        context.lineTo(lineX, context.canvas.height);
        context.stroke();

        lineX += 50;
    }

    while (lineY < context.canvas.height) {
        context.beginPath();
        context.moveTo(0, lineY);
        context.lineTo(context.canvas.width, lineY);
        context.stroke();

        lineY += 50;
    }
}

var attachMouseListeners = function (canvas) {
    var handleTouches = function (event) {
        if (typeof event.changedTouches != "undefined") {
            var lastTouch = event.changedTouches[event.changedTouches.length - 1];

            var canvasMouseX = lastTouch.pageX;
            var canvasMouseY = lastTouch.pageY;

            mousePos = [canvasMouseX, canvasMouseY];
        }
    };

    var handleMouseDown = function (event) {
        handleTouches(event);

        mouseDown = true;
    };

    var handleMouseUp = function () {
        mouseDown = false;
    };

    var handleMouseMove = function (event) {
        var canvasMouseX = event.clientX - (canvas.offsetLeft - window.pageXOffset);
        var canvasMouseY = event.clientY - (canvas.offsetTop - window.pageYOffset);

        mousePos = [canvasMouseX, canvasMouseY];
    };

    canvas.onmousedown = handleMouseDown;
    canvas.onmouseup = handleMouseUp;
    canvas.onmousemove = handleMouseMove;

    document.body.ontouchstart = handleMouseDown;
    document.body.ontouchend = handleMouseUp;
    document.body.ontouchcancel = handleMouseUp;
    document.body.ontouchmove = handleTouches;
};

var send = function (color, x, y, isNew) {
    if (socketOpen) {
        var message = player + '>draw>' + [color, Math.round(x), Math.round(y), isNew ? 1 : 0].join('|');

        socket.send(message);
    }
};

var receive = function (data) {
    var chunks = data.split('>');
    var recPlayer = chunks[0];
    
    var action = chunks[1];
    var body = chunks[2];

    if (recPlayer == player && action == "moved") {
        room = chunks[2];
        
        clearLocal();
    }

    if (action == "clear") {
        clearLocal();

        return;
    }

    var chunks = body.split('|');

    var playerColor = chunks[0];
    var x = chunks[1];
    var y = chunks[2];
    var isNew = chunks[3] == 1 ? true : false;

    if (typeof players[playerColor] == "undefined") {
        players[playerColor] = [];
    }

    if (isNew || players[playerColor].length == 0) {
        players[playerColor].push([]);
    }

    players[playerColor][players[playerColor].length - 1].push([x, y]);
};

var openSocket = function () {
    socket = new WebSocket('ws://' + host + '/Canvas/Server');
    socket.onopen = function (event) {
        socketOpen = true;

        socket.send(player + '>load>');
    };

    socket.onclose = function (event) {
        socketOpen = false;
    }

    socket.onmessage = function (event) {
        receive(event.data);
    };
};

var sizeCanvas = function (canvas) {
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
}

var attachResizeListener = function (canvas) {
    window.onresize = function () { sizeCanvas(canvas); };
}

var setup = function (canvas) {
    sizeCanvas(canvas);

    color = getColor();

    document.getElementById('color').value = color;

    players[color] = [];

    attachKeyListener();
    attachMouseListeners(canvas);
    attachColorChangeListener();
    attachResizeListener(canvas);
    openSocket();
};

var handleMouse = function () {
    if (mouseDown) {

        if (!mouseWasDown) {
            players[color].push([]);
        }

        var offsetPos = [mousePos[0]+offsetX, mousePos[1]+offsetY];

        players[color][players[color].length - 1].push(offsetPos);

        send(color, offsetPos[0], offsetPos[1], !mouseWasDown);

        mouseWasDown = true;
    } else {
        mouseWasDown = false;
    }
};

var draw = function (context) {
    var canvas = context.canvas;

    context.clearRect(0, 0, canvas.width, canvas.height);

    if (drawGrid) {
        drawGridLines(context);
    }

    context.setTransform(1, 0, 0, 1, -offsetX, -offsetY);
        
    for (var color in players) {

        for (var segment in players[color]) {
            var counter = 0;
            var segmentLength = players[color][segment].length;

            if (segmentLength > 1) {
                context.beginPath();
                context.strokeStyle = color;

                while (counter < segmentLength) {
                    if (counter == 0) {
                        context.moveTo(players[color][segment][counter][0], players[color][segment][counter][1])
                    } else {
                        context.lineTo(players[color][segment][counter][0], players[color][segment][counter][1]);
                    }

                    counter++;
                }

                context.stroke();
            }

        }
    }

    context.setTransform(1, 0, 0, 1, 0, 0);

    drawHud(context);

    handleMouse();
};

var ready = function () {
    player = getHex(16);

    var canvasId = 'game';

    var canvas = document.getElementById(canvasId);

    setup(canvas);

    var context = getContext(canvas);
    
    var mills = 1000;

    var fps = 30;

    setInterval(function () { draw(context); }, Math.floor(mills/fps)); 
};
