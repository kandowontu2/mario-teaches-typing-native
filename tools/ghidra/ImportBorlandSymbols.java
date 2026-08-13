// Imports the Turbo Debugger symbols appended to the original DOS executable.
// MARCD.EXE keeps the old Borland 0x52fb table after its MZ load image, which
// gives this source-level port authoritative names and far addresses even
// though Ghidra does not parse the table itself.

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.symbol.SourceType;

import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;

public class ImportBorlandSymbols extends GhidraScript {
    @Override
    public void run() throws Exception {
        String executablePath = currentProgram.getExecutablePath();
        if (executablePath.matches("^/[A-Za-z]:/.*")) {
            executablePath = executablePath.substring(1);
        }
        byte[] file = Files.readAllBytes(Path.of(executablePath));
        int pages = u16(file, 4);
        int lastPageBytes = u16(file, 2);
        int debugBase = lastPageBytes == 0
            ? pages * 512
            : (pages - 1) * 512 + lastPageBytes;
        if (u16(file, debugBase) != 0x52fb) {
            throw new IllegalArgumentException("Executable has no Borland 0x52fb debug table.");
        }

        int symbolCount = u16(file, debugBase + 0x0e);
        int nameCount = u16(file, debugBase + 0x08);
        int namePool = debugBase + u32(file, debugBase + 0x3e);
        byte[] firstName = "__setargv__\0".getBytes(StandardCharsets.US_ASCII);
        int stringPool = find(file, firstName, namePool);
        if (stringPool < 0) {
            throw new IllegalArgumentException("Borland name-string pool was not found.");
        }

        List<String> names = new ArrayList<>();
        int cursor = stringPool;
        while (cursor < file.length && names.size() < nameCount) {
            int end = cursor;
            while (end < file.length && file[end] != 0) {
                end++;
            }
            names.add(new String(file, cursor, end - cursor, StandardCharsets.US_ASCII));
            cursor = end + 1;
        }
        if (names.size() != nameCount) {
            throw new IllegalArgumentException("Borland name pool ended early.");
        }

        int records = debugBase + 0x50;
        int imported = 0;
        for (int index = 0; index < symbolCount; index++) {
            int record = records + index * 9;
            int nameIndex = u16(file, record);
            int offset = u16(file, record + 4);
            int segment = u16(file, record + 6);
            // Borland name IDs are one-based; zero means no public name.
            if (nameIndex == 0 || nameIndex > names.size() || segment == 0) {
                continue;
            }
            String name = names.get(nameIndex - 1).replaceAll("[^A-Za-z0-9_@$?]", "_");
            if (name.isEmpty()) {
                continue;
            }

            // Ghidra's DOS loader places the MZ image at PSP segment 0x1000.
            long linear = ((long) segment + 0x1000L) * 16L + offset;
            Address address = currentProgram.getAddressFactory()
                .getDefaultAddressSpace().getAddress(linear);
            if (currentProgram.getMemory().contains(address)) {
                try {
                    createLabel(address, name, true, SourceType.IMPORTED);
                    imported++;
                } catch (Exception ignored) {
                    // Aliases and duplicate linker records are expected.
                }
            }
        }
        println("Imported " + imported + " Borland symbols from " + symbolCount + " records.");
    }

    private static int u16(byte[] data, int offset) {
        return (data[offset] & 0xff) | ((data[offset + 1] & 0xff) << 8);
    }

    private static int u32(byte[] data, int offset) {
        return u16(data, offset) | (u16(data, offset + 2) << 16);
    }

    private static int find(byte[] haystack, byte[] needle, int start) {
        outer:
        for (int index = start; index <= haystack.length - needle.length; index++) {
            for (int item = 0; item < needle.length; item++) {
                if (haystack[index + item] != needle[item]) {
                    continue outer;
                }
            }
            return index;
        }
        return -1;
    }
}
