// Finds defined string data containing any supplied fragment and prints its
// instruction references. Usage: -postScript FindStringsAndReferences.java fir1 endf

import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.Data;
import ghidra.program.model.listing.Function;
import ghidra.program.model.symbol.Reference;

public class FindStringsAndReferences extends GhidraScript {
    @Override
    public void run() throws Exception {
        if (getScriptArgs().length == 0) {
            printerr("Supply at least one case-insensitive string fragment.");
            return;
        }

        for (Data data : currentProgram.getListing().getDefinedData(true)) {
            Object value = data.getValue();
            if (!(value instanceof String text) || !matches(text)) {
                continue;
            }
            println("STRING " + data.getAddress() + " " + text);
            for (Reference reference : getReferencesTo(data.getAddress())) {
                Function function = currentProgram.getFunctionManager()
                    .getFunctionContaining(reference.getFromAddress());
                println("REF " + reference.getReferenceType() + " from " +
                    reference.getFromAddress() + " in " +
                    (function == null ? "<no function>" : function.getName()));
            }
        }
    }

    private boolean matches(String text) {
        for (String fragment : getScriptArgs()) {
            if (text.toLowerCase().contains(fragment.toLowerCase())) {
                return true;
            }
        }
        return false;
    }
}
