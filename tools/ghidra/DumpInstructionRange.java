// Prints an exact segmented instruction range and annotates flow targets.
// Usage: -postScript DumpInstructionRange.java 18e3:026a 18e3:0338

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.symbol.Symbol;

public class DumpInstructionRange extends GhidraScript {
    @Override
    public void run() throws Exception {
        if (getScriptArgs().length != 2) {
            printerr("Supply an inclusive start address and exclusive end address.");
            return;
        }
        Address start = currentProgram.getAddressFactory().getAddress(getScriptArgs()[0]);
        Address end = currentProgram.getAddressFactory().getAddress(getScriptArgs()[1]);
        if (start == null || end == null) {
            printerr("Could not parse an address.");
            return;
        }
        disassemble(start);
        println("===== " + start + " through " + end + " =====");
        for (Instruction instruction = currentProgram.getListing().getInstructionAt(start);
             instruction != null && instruction.getAddress().compareTo(end) < 0;
             instruction = instruction.getNext()) {
            StringBuilder targets = new StringBuilder();
            for (Address flow : instruction.getFlows()) {
                Symbol symbol = currentProgram.getSymbolTable().getPrimarySymbol(flow);
                Function targetFunction = currentProgram.getFunctionManager().getFunctionAt(flow);
                String name = symbol != null ? symbol.getName() :
                    targetFunction != null ? targetFunction.getName() : null;
                if (name != null) {
                    targets.append(targets.length() == 0 ? "  -> " : ", ").append(name);
                }
            }
            println(instruction.getAddress() + "  " + instruction + targets);
        }
    }
}
