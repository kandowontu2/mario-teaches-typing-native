// Prints instruction references to exact addresses.
// Usage: -postScript DumpAddressReferences.java 367a:559a 367a:5512

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.symbol.Reference;

public class DumpAddressReferences extends GhidraScript {
    @Override
    public void run() throws Exception {
        for (String argument : getScriptArgs()) {
            Address address = currentProgram.getAddressFactory().getAddress(argument);
            if (address == null) {
                printerr("Invalid address: " + argument);
                continue;
            }
            println("===== ADDRESS " + address + " =====");
            for (Reference reference : getReferencesTo(address)) {
                Function function = currentProgram.getFunctionManager()
                    .getFunctionContaining(reference.getFromAddress());
                println("REF " + reference.getReferenceType() + " from " +
                    reference.getFromAddress() + " in " +
                    (function == null ? "<no function>" : function.getName()));
            }
        }
    }
}
