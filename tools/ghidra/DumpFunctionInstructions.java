// Prints the decoded instructions for named Borland symbols in MARCD.EXE.

import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.address.Address;
import ghidra.program.model.symbol.Symbol;
import ghidra.program.model.symbol.SymbolTable;

public class DumpFunctionInstructions extends GhidraScript {
    @Override
    public void run() throws Exception {
        for (String argument : getScriptArgs()) {
            if (!argument.matches("(?i)(0x)?[0-9a-f]+(:[0-9a-f]+)?")) {
                continue;
            }
            long linear;
            if (argument.contains(":")) {
                String[] pieces = argument.split(":", 2);
                linear = Long.parseLong(pieces[0], 16) * 16L + Long.parseLong(pieces[1], 16);
            } else {
                linear = Long.parseLong(argument.replaceFirst("(?i)^0x", ""), 16);
            }
            Address address = currentProgram.getAddressFactory().getDefaultAddressSpace().getAddress(linear);
            Function function = currentProgram.getFunctionManager().getFunctionContaining(address);
            if (function == null) {
                disassemble(address);
                function = createFunction(address, null);
            }
            if (function != null) {
                dumpFunction(function, argument);
            }
        }

        for (Symbol symbol : currentProgram.getSymbolTable().getAllSymbols(true)) {
            for (String fragment : getScriptArgs()) {
                if (!symbol.getName().equalsIgnoreCase(fragment)) {
                    continue;
                }
                Function function = currentProgram.getFunctionManager()
                    .getFunctionContaining(symbol.getAddress());
                if (function == null) {
                    disassemble(symbol.getAddress());
                    function = createFunction(symbol.getAddress(), symbol.getName());
                }
                if (function == null) {
                    printerr("No function for " + symbol.getName());
                    continue;
                }
                dumpFunction(function, symbol.getName());
            }
        }
    }

    private void dumpFunction(Function function, String label) {
        println("===== " + label + " / " + function.getName() + " @ " + function.getEntryPoint() + " =====");
        for (Instruction instruction : currentProgram.getListing().getInstructions(function.getBody(), true)) {
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
