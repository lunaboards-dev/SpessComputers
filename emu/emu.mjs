import {Terminal} from "./node_modules/@xterm/xterm/lib/xterm.mjs";

const term = new Terminal({
    cols: 80,
    rows: 60
});

term.open(document.getElementById("container"));

let ws;
function connect_tty(id) {
    if (ws) {
        ws.close();
    }
    ws = new WebSocket("ws://127.0.0.1:42069/tty");
    ws.onmessage = (msg) => {
        msg.data.bytes().then((bytes) => {
            term.write(bytes)
        })
    }
    term.onData((data) => {
        ws.send("\x01"+data);
    })
    ws.onopen = () => {
        ws.send("\x00"+id);
    }
    ws.onclose = () => {
        term.writeln("Closed.");
    }
}

const ctl = new WebSocket("ws://127.0.0.1:42069/ctl");
ctl.onmessage = (msg) => {
    msg.data.text().then((txt) => {
        console.log(txt);
        const obj = JSON.parse(txt);
        if (obj.command == "new_computer") {
            term.writeln("Connected to TTY "+obj.id);
            connect_tty(obj.id);
        }
    });
}

document.getElementById("new").onclick = () => {
    ctl.send(JSON.stringify({command: "new_computer"}))
}

document.getElementById("pwr").onclick = () => {
    if (ws) {
        ws.send("\x02"+JSON.stringify({command: "power", hard: false}));
    }
}

document.getElementById("hpwr").onclick = () => {
    if (ws) {
        ws.send("\x02"+JSON.stringify({command: "power", hard: true}));
    }
}