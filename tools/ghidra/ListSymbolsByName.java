// Lists symbols/functions whose names contain any supplied fragment.
// Usage: -postScript ListSymbolsByName.java teacher report custom

import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.Function;
import ghidra.program.model.symbol.Symbol;

import java.util.HashSet;
import java.util.Set;

public class ListSymbolsByName extends GhidraScript {
    @Override
    public void run() throws Exception {
        Set<String> emitted = new HashSet<>();
        for (Symbol symbol : currentProgram.getSymbolTable().getAllSymbols(true)) {
            if (!matches(symbol.getName())) {
                continue;
            }
            String line = "SYMBOL " + symbol.getName() + " @ " + symbol.getAddress();
            if (emitted.add(line)) {
                println(line);
            }
        }
        for (Function function : currentProgram.getFunctionManager().getFunctions(true)) {
            if (!matches(function.getName())) {
                continue;
            }
            String line = "FUNCTION " + function.getName() + " @ " + function.getEntryPoint();
            if (emitted.add(line)) {
                println(line);
            }
        }
    }

    private boolean matches(String name) {
        for (String fragment : getScriptArgs()) {
            if (name.toLowerCase().contains(fragment.toLowerCase())) {
                return true;
            }
        }
        return false;
    }
}
