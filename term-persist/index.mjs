import pkg from '@xterm/headless';
const { Terminal } = pkg;
import { SerializeAddon } from "@xterm/addon-serialize";

import process from 'node:process';

const term = new Terminal({
    cols: 80,
    rows: 25,
    allowProposedApi: true
});

const serializeAddon = new SerializeAddon();
term.loadAddon(serializeAddon);

process.stdin.on('data', (data) => {
    term.write(data);
})

process.stdin.on('end', () => {
    process.exit(0);
})

process.on('SIGINT', () => {
    console.log(serializeAddon.serialize())
})