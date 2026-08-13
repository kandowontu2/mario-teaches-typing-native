// Finds instructions whose operands contain any supplied scalar and prints
// a small disassembly window. Usage:
//   -postScript FindScalarReferences.java 4e20 4f4c 5014

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.scalar.Scalar;

import java.util.HashSet;
import java.util.Set;

public class FindScalarReferences extends GhidraScript {
    @Override
    public void run() throws Exception {
        Set<Long> requested = new HashSet<>();
        for (String argument : getScriptArgs()) {
            requested.add(Long.parseLong(argument.replaceFirst("(?i)^0x", ""), 16));
        }
        if (requested.isEmpty()) {
            printerr("Supply at least one hexadecimal scalar.");
            return;
        }

        for (Instruction instruction : currentProgram.getListing().getInstructions(true)) {
            boolean matched = false;
            for (int operand = 0; operand < instruction.getNumOperands() && !matched; operand++) {
                for (Object object : instruction.getOpObjects(operand)) {
                    if (object instanceof Scalar scalar && requested.contains(scalar.getUnsignedValue())) {
                        matched = true;
                        break;
                    }
                }
            }
            if (!matched) {
                continue;
            }

            Function function = currentProgram.getFunctionManager()
                .getFunctionContaining(instruction.getAddress());
            println("===== " + instruction.getAddress() + " in " +
                (function == null ? "<no function>" : function.getName()) + " =====");
            Instruction first = instruction;
            for (int before = 0; before < 12 && first.getPrevious() != null; before++) {
                first = first.getPrevious();
            }
            Instruction cursor = first;
            for (int count = 0; count < 25 && cursor != null; count++, cursor = cursor.getNext()) {
                Address address = cursor.getAddress();
                println((address.equals(instruction.getAddress()) ? "> " : "  ") +
                    address + "  " + cursor);
            }
        }
    }
}
