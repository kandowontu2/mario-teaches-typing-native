// Lists imported memory blocks and overlay metadata.

import ghidra.app.script.GhidraScript;
import ghidra.program.model.mem.MemoryBlock;

public class ListMemoryBlocks extends GhidraScript {
    @Override
    public void run() throws Exception {
        for (MemoryBlock block : currentProgram.getMemory().getBlocks()) {
            println(block.getName() + " " + block.getStart() + " " + block.getEnd() +
                " overlay=" + block.isOverlay() + " source=" + block.getSourceName());
        }
    }
}
