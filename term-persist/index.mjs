import { Terminal } from '@xterm/headless';
import { SerializeAddon } from "@xterm/addon-serialize";

const process = require('node:process');

const term = new Terminal({
    cols: 80,
    rows: 25
});

const serializeAddon = new SerializeAddon();
term.loadAddon(serializeAddon);

process.stdin.on('data', (data) => {
    term.write(data);
})

process.stdin.on('end', () => {
    process.abort();
})

process.on('SIGINT', () => {
    console.log(serializeAddon.serialize())
})