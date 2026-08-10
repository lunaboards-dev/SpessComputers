import {Terminal} from "./node_modules/@xterm/xterm/lib/xterm.mjs";
import { ImageAddon } from "./node_modules/@xterm/addon-image/lib/addon-image.mjs";

const term = new Terminal({
    cols: 80,
    rows: 25
});

term.loadAddon(new ImageAddon());
term.open(document.getElementById("container"));

//let queue = [];

async function* queueProcessor() {
    while (true) {
        const msg = yield
        if (msg) {
            const bytes = await msg.data.bytes()
            term.write(bytes);
        }
    }
}

let queue = queueProcessor();

/* function procQueue() {
    let first = queue.splice(0,1);
    if (first.length > 0) {
        let obj = first[0]
        obj.msg.data.bytes().then((bytes) => {
            obj.bytes = bytes;
        })
    }
    
} */

let ws;
let id = 0;
function connect_tty(id) {
    if (ws) {
        ws.close();
    }
    ws = new WebSocket("ws://127.0.0.1:42069/tty");
    ws.onmessage = async (msg) => {
        await queue.next(msg);
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
            term.writeln("\x1b[2J\x1b[HConnected to TTY "+obj.id);
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

document.getElementById("res").onclick = () => {
    if (ws) {
        ws.send("\x02"+JSON.stringify({command: "resume"}));
    }
}

document.getElementById("hpwr").onclick = () => {
    if (ws) {
        ws.send("\x02"+JSON.stringify({command: "power", hard: true}));
    }
}

await queue.next();