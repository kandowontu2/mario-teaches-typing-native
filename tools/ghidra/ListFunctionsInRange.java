// Lists function entries and bodies that overlap an exact segmented range.
// Usage: -postScript ListFunctionsInRange.java 1f8e:0000 1f8e:1834

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;

public class ListFunctionsInRange extends GhidraScript {
    @Override
    public void run() throws Exception {
        if (getScriptArgs().length != 2) {
            printerr("Supply inclusive start and exclusive end addresses.");
            return;
        }
        Address start = currentProgram.getAddressFactory().getAddress(getScriptArgs()[0]);
        Address end = currentProgram.getAddressFactory().getAddress(getScriptArgs()[1]);
        if (start == null || end == null) {
            printerr("Could not parse an address.");
            return;
        }

        for (Function function : currentProgram.getFunctionManager().getFunctions(start, true)) {
            if (function.getEntryPoint().compareTo(end) >= 0) {
                break;
            }
            println(function.getEntryPoint() + " " + function.getName() +
                " body=" + function.getBody().getMinAddress() + ".." +
                function.getBody().getMaxAddress() +
                " bytes=" + function.getBody().getNumAddresses());
        }
    }
}
