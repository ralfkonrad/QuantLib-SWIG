# BuildJar.cmake — Helper script to compile Java sources and create a jar
#
# Runs in script mode (cmake -P) so that the .java files SWIG generates can be
# enumerated at build time rather than at configure time.
#
# Required variables:
#   JAVA_COMPILER       — Path to javac
#   JAVA_SOURCE_DIR     — Directory containing the generated .java files
#   JAVA_BIN_DIR        — Output directory for .class files
#   JAVA_JAR_EXECUTABLE — Path to jar
#   JAR_OUTPUT          — Output jar file path
# Optional:
#   JAVAC_FLAGS         — ";"-separated flags for javac

if (NOT JAVA_COMPILER OR NOT JAVA_SOURCE_DIR OR NOT JAVA_BIN_DIR
    OR NOT JAVA_JAR_EXECUTABLE OR NOT JAR_OUTPUT)
    message(FATAL_ERROR "BuildJar.cmake: Missing required variables")
endif()

# Start from a clean class output directory so that classes belonging to
# wrappers that are no longer generated cannot end up in the jar.
file(REMOVE_RECURSE "${JAVA_BIN_DIR}")
file(MAKE_DIRECTORY "${JAVA_BIN_DIR}")

file(GLOB JAVA_SOURCES "${JAVA_SOURCE_DIR}/*.java")
list(LENGTH JAVA_SOURCES _count)
if (_count EQUAL 0)
    message(FATAL_ERROR "BuildJar.cmake: No .java files found in ${JAVA_SOURCE_DIR}")
endif()
message(STATUS "Compiling ${_count} Java source files...")

# The generated wrappers are numerous enough to overflow the command line on
# some platforms, so pass them to javac through an argument file. This is the
# equivalent of the `find ... | xargs javac` in Java/Makefile.am.
set(_argfile "${JAVA_BIN_DIR}/javac-sources.txt")
set(_argfile_content "")
foreach(_source ${JAVA_SOURCES})
    string(APPEND _argfile_content "\"${_source}\"\n")
endforeach()
file(WRITE "${_argfile}" "${_argfile_content}")

execute_process(
    COMMAND ${JAVA_COMPILER} ${JAVAC_FLAGS} -d "${JAVA_BIN_DIR}" "@${_argfile}"
    RESULT_VARIABLE _result
)
if (_result)
    message(FATAL_ERROR "javac failed with exit code ${_result}")
endif()

file(REMOVE "${_argfile}")

# Create the jar, replacing any previous one
file(REMOVE "${JAR_OUTPUT}")
execute_process(
    COMMAND ${JAVA_JAR_EXECUTABLE} cf "${JAR_OUTPUT}"
        -C "${JAVA_BIN_DIR}" org
    RESULT_VARIABLE _result
)
if (_result)
    message(FATAL_ERROR "jar failed with exit code ${_result}")
endif()

message(STATUS "Created ${JAR_OUTPUT}")
