SOURCES=src/jit2_bf.cpp src/jit_bf.cpp src/interpreter_bf.cpp
BINARIES=$(SOURCES:.cpp=)
CXXFLAGS?=-O3 -Wall -Wextra

.PHONY: all clean

all: $(BINARIES)

clean:
	$(RM) $(BINARIES)
