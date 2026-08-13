// Ghidra headless helper for documenting the original DOS executable while
// replacing its routines with native code. Usage:
//   -postScript DumpNamedFunctions.java name-fragment [name-fragment ...]

import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileResults;
import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.Function;
import ghidra.program.model.address.Address;
import ghidra.program.model.symbol.Symbol;

public class DumpNamedFunctions extends GhidraScript {
    @Override
    public void run() throws Exception {
        if (getScriptArgs().length == 0) {
            printerr("Supply at least one function-name fragment.");
            return;
        }

        DecompInterface decompiler = new DecompInterface();
        decompiler.openProgram(currentProgram);
        try {
            for (String argument : getScriptArgs()) {
                if (!argument.matches("(?i)(0x)?[0-9a-f]+(:[0-9a-f]+)?")) {
                    continue;
                }
                Address address;
                if (argument.contains(":")) {
                    address = currentProgram.getAddressFactory().getAddress(argument);
                } else {
                    long linear = Long.parseLong(argument.replaceFirst("(?i)^0x", ""), 16);
                    address = currentProgram.getAddressFactory().getDefaultAddressSpace().getAddress(linear);
                }
                if (address == null) {
                    printerr("Could not parse address " + argument + ".");
                    continue;
                }
                Function function = currentProgram.getFunctionManager().getFunctionContaining(address);
                if (function == null) {
                    disassemble(address);
                    function = createFunction(address, null);
                }
                if (function == null) {
                    printerr("No function could be created at " + argument + " (address " + address + ").");
                    continue;
                }
                dumpFunction(decompiler, function);
            }

            for (Symbol symbol : currentProgram.getSymbolTable().getAllSymbols(true)) {
                String name = symbol.getName();
                boolean matches = false;
                for (String fragment : getScriptArgs()) {
                    if (name.toLowerCase().contains(fragment.toLowerCase())) {
                        matches = true;
                        break;
                    }
                }
                if (!matches) {
                    continue;
                }
                Function function = currentProgram.getFunctionManager().getFunctionContaining(symbol.getAddress());
                if (function == null) {
                    disassemble(symbol.getAddress());
                    function = createFunction(symbol.getAddress(), name);
                }
                if (function != null) {
                    dumpFunction(decompiler, function);
                }
            }

            for (Function function : currentProgram.getFunctionManager().getFunctions(true)) {
                String name = function.getName();
                boolean matches = false;
                for (String fragment : getScriptArgs()) {
                    if (name.toLowerCase().contains(fragment.toLowerCase())) {
                        matches = true;
                        break;
                    }
                }
                if (!matches) {
                    continue;
                }
                dumpFunction(decompiler, function);
            }
        } finally {
            decompiler.dispose();
        }
    }

    private void dumpFunction(DecompInterface decompiler, Function function) {
        println("===== " + function.getName() + " @ " + function.getEntryPoint() + " =====");
        DecompileResults result = decompiler.decompileFunction(function, 120, monitor);
        if (result.decompileCompleted()) {
            println(result.getDecompiledFunction().getC());
        } else {
            println("Decompiler error: " + result.getErrorMessage());
        }
    }
}
