# BuildJar.cmake — Helper script to compile Java sources and create a jar
#
# Required variables:
#   JAVA_COMPILER       — Path to javac
#   JAVA_SOURCE_DIR     — Directory containing .java files
#   JAVA_BIN_DIR        — Output directory for .class files
#   JAVA_JAR_EXECUTABLE — Path to jar
#   JAR_OUTPUT          — Output jar file path

if (NOT JAVA_COMPILER OR NOT JAVA_SOURCE_DIR OR NOT JAVA_BIN_DIR
    OR NOT JAVA_JAR_EXECUTABLE OR NOT JAR_OUTPUT)
    message(FATAL_ERROR "BuildJar.cmake: Missing required variables")
endif()

# Find all .java files
file(GLOB JAVA_SOURCES "${JAVA_SOURCE_DIR}/*.java")
list(LENGTH JAVA_SOURCES _count)
if (_count EQUAL 0)
    message(FATAL_ERROR "BuildJar.cmake: No .java files found in ${JAVA_SOURCE_DIR}")
endif()
message(STATUS "Compiling ${_count} Java source files...")

# Compile
execute_process(
    COMMAND ${JAVA_COMPILER} -source 8 -target 8 -O -g
        -d "${JAVA_BIN_DIR}"
        ${JAVA_SOURCES}
    RESULT_VARIABLE _result
)
if (_result)
    message(FATAL_ERROR "javac failed with exit code ${_result}")
endif()

# Create jar
execute_process(
    COMMAND ${JAVA_JAR_EXECUTABLE} cf "${JAR_OUTPUT}"
        -C "${JAVA_BIN_DIR}" org
    RESULT_VARIABLE _result
)
if (_result)
    message(FATAL_ERROR "jar failed with exit code ${_result}")
endif()

message(STATUS "Created ${JAR_OUTPUT}")
