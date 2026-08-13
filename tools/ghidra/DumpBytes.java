// Prints raw program bytes without relying on Ghidra's existing instruction
// boundaries. Usage: -postScript DumpBytes.java 1f8e:0ed7 512

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;

public class DumpBytes extends GhidraScript {
    @Override
    public void run() throws Exception {
        if (getScriptArgs().length != 2) {
            printerr("Supply a start address and byte count.");
            return;
        }
        Address start = currentProgram.getAddressFactory().getAddress(getScriptArgs()[0]);
        int count = Integer.parseInt(getScriptArgs()[1]);
        if (start == null || count < 1) {
            printerr("Invalid start address or byte count.");
            return;
        }
        byte[] bytes = new byte[count];
        int read = currentProgram.getMemory().getBytes(start, bytes);
        StringBuilder line = new StringBuilder(read * 2);
        for (int index = 0; index < read; index++) {
            line.append(String.format("%02x", bytes[index] & 0xff));
        }
        println("BYTES " + start + " " + line);
    }
}
