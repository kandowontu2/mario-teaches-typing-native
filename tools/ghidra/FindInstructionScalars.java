// Prints instructions containing any supplied scalar value, in hex or decimal.
// Usage: -postScript FindInstructionScalars.java 0x275f 0x276f

import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.scalar.Scalar;

import java.util.HashSet;
import java.util.Set;

public class FindInstructionScalars extends GhidraScript {
    @Override
    public void run() throws Exception {
        Set<Long> targets = new HashSet<>();
        for (String argument : getScriptArgs()) {
            targets.add(Long.decode(argument));
        }
        for (Instruction instruction : currentProgram.getListing().getInstructions(true)) {
            for (int operand = 0; operand < instruction.getNumOperands(); operand++) {
                for (Object object : instruction.getOpObjects(operand)) {
                    if (object instanceof Scalar scalar && targets.contains(scalar.getUnsignedValue())) {
                        Function function = currentProgram.getFunctionManager()
                            .getFunctionContaining(instruction.getAddress());
                        println(instruction.getAddress() + " " + instruction + " in " +
                            (function == null ? "<no function>" : function.getName()));
                    }
                }
            }
        }
    }
}
