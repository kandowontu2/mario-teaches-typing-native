// Prints only the instruction references to exact named Borland symbols.
// Usage: -postScript DumpSymbolReferencesBrief.java _gbl_bonus_shape ...

import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.Function;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.Symbol;

public class DumpSymbolReferencesBrief extends GhidraScript {
    @Override
    public void run() throws Exception {
        if (getScriptArgs().length == 0) {
            printerr("Supply at least one exact symbol name.");
            return;
        }

        for (String requested : getScriptArgs()) {
            for (Symbol symbol : currentProgram.getSymbolTable().getAllSymbols(true)) {
                if (!symbol.getName().equalsIgnoreCase(requested)) {
                    continue;
                }
                println("===== SYMBOL " + symbol.getName() + " @ " + symbol.getAddress() + " =====");
                for (Reference reference : getReferencesTo(symbol.getAddress())) {
                    Function function = currentProgram.getFunctionManager()
                        .getFunctionContaining(reference.getFromAddress());
                    println("REF " + reference.getReferenceType() + " from " +
                        reference.getFromAddress() + " in " +
                        (function == null ? "<no function>" : function.getName()));
                }
            }
        }
    }
}
