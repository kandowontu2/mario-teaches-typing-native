// Prints references to named Borland symbols and decompiles each referring
// function once. This is useful for recovering the rules behind global state
// whose symbol itself is data rather than code.

import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileResults;
import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.Function;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.Symbol;

import java.util.LinkedHashSet;
import java.util.Set;

public class DumpSymbolReferences extends GhidraScript {
    @Override
    public void run() throws Exception {
        if (getScriptArgs().length == 0) {
            printerr("Supply at least one exact symbol name.");
            return;
        }

        DecompInterface decompiler = new DecompInterface();
        decompiler.openProgram(currentProgram);
        try {
            for (String requested : getScriptArgs()) {
                for (Symbol symbol : currentProgram.getSymbolTable().getAllSymbols(true)) {
                    if (!symbol.getName().equalsIgnoreCase(requested)) {
                        continue;
                    }
                    println("===== SYMBOL " + symbol.getName() + " @ " + symbol.getAddress() + " =====");
                    Set<Function> functions = new LinkedHashSet<>();
                    for (Reference reference : getReferencesTo(symbol.getAddress())) {
                        Function function = currentProgram.getFunctionManager()
                            .getFunctionContaining(reference.getFromAddress());
                        println("REF " + reference.getReferenceType() + " from " +
                            reference.getFromAddress() + " in " +
                            (function == null ? "<no function>" : function.getName()));
                        if (function != null) {
                            functions.add(function);
                        }
                    }
                    for (Function function : functions) {
                        println("----- " + function.getName() + " @ " + function.getEntryPoint() + " -----");
                        DecompileResults result = decompiler.decompileFunction(function, 120, monitor);
                        if (result.decompileCompleted()) {
                            println(result.getDecompiledFunction().getC());
                        } else {
                            println("Decompiler error: " + result.getErrorMessage());
                        }
                    }
                }
            }
        } finally {
            decompiler.dispose();
        }
    }
}
