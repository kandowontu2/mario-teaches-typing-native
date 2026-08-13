// Disassembles (where necessary) and prints instructions in an address range.
// Usage: -postScript DumpInstructions.java 18e3:23d0 18e3:2440

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Instruction;

public class DumpInstructions extends GhidraScript {
    @Override
    public void run() throws Exception {
        if (getScriptArgs().length != 2) {
            printerr("Supply inclusive start and end addresses.");
            return;
        }
        Address start = currentProgram.getAddressFactory().getAddress(getScriptArgs()[0]);
        Address end = currentProgram.getAddressFactory().getAddress(getScriptArgs()[1]);
        if (start == null || end == null || end.compareTo(start) < 0) {
            printerr("Invalid address range.");
            return;
        }
        disassemble(start);
        for (Instruction instruction : currentProgram.getListing().getInstructions(start, true)) {
            if (instruction.getAddress().compareTo(end) > 0) {
                break;
            }
            println(instruction.getAddress() + "  " + instruction);
        }
    }
}
