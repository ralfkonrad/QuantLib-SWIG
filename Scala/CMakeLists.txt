# Scala examples for QuantLib-SWIG
#
# Scala has no SWIG binding of its own — it uses the JNI library and
# QuantLib.jar built by the Java subdirectory.

find_program(SCALAC_EXECUTABLE scalac REQUIRED)
find_program(SCALA_EXECUTABLE scala REQUIRED)
message(STATUS "Found scalac: ${SCALAC_EXECUTABLE}")

set(SCALA_EXAMPLES
    CPIBond
    EquityOptions
    HestonModelCalibration
    HestonMonteCarlo
    ObserverPattern
    Optimizer
    RandomNumbers
    Swing
)

if (NOT TARGET QuantLib-Java-jar)
    message(FATAL_ERROR "Scala examples require the Java binding (QuantLib-Java-jar target).")
endif()

set(SCALA_JAVA_BINARY_DIR "${CMAKE_BINARY_DIR}/Java")
set(SCALA_QUANTLIB_JAR "${SCALA_JAVA_BINARY_DIR}/QuantLib.jar")
set(SCALA_BIN_DIR "${CMAKE_CURRENT_BINARY_DIR}/examples")

file(MAKE_DIRECTORY "${SCALA_BIN_DIR}")

set(SCALA_CLASS_FILES)
foreach(_example ${SCALA_EXAMPLES})
    set(_src "${CMAKE_CURRENT_SOURCE_DIR}/examples/${_example}.scala")
    set(_class "${SCALA_BIN_DIR}/${_example}.class")

    add_custom_command(
        OUTPUT "${_class}"
        COMMAND "${SCALAC_EXECUTABLE}"
            -cp "${SCALA_QUANTLIB_JAR}"
            -language:postfixOps
            -d "${SCALA_BIN_DIR}"
            "${_src}"
        DEPENDS "${_src}" QuantLib-Java-jar "${SCALA_QUANTLIB_JAR}"
        COMMENT "Compiling Scala example: ${_example}"
        VERBATIM
    )
    list(APPEND SCALA_CLASS_FILES "${_class}")

    # Mirrors Scala/Makefile.am's check-local
    add_test(NAME scala-example-${_example}
        COMMAND "${SCALA_EXECUTABLE}"
            -cp "${SCALA_QUANTLIB_JAR}${JAVA_CLASSPATH_SEPARATOR}${SCALA_BIN_DIR}"
            examples.${_example})
    set_tests_properties(scala-example-${_example} PROPERTIES
        ENVIRONMENT "LD_LIBRARY_PATH=${SCALA_JAVA_BINARY_DIR};DYLD_LIBRARY_PATH=${SCALA_JAVA_BINARY_DIR}")
endforeach()

add_custom_target(QuantLib-Scala-examples ALL DEPENDS ${SCALA_CLASS_FILES})
