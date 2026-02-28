# Scala examples for QuantLib-SWIG
#
# Scala does not have its own SWIG binding — it uses the Java JNI
# library and QuantLib.jar built by the Java subdirectory.
# This CMakeLists.txt compiles and optionally runs the Scala examples.

find_program(SCALAC_EXECUTABLE scalac)
find_program(SCALA_EXECUTABLE scala)

if (NOT SCALAC_EXECUTABLE)
    message(FATAL_ERROR "scalac not found. Please install Scala or set SCALAC_EXECUTABLE.")
endif()
message(STATUS "Found scalac: ${SCALAC_EXECUTABLE}")

# Scala examples
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

# The Java jar must be built first
if (NOT TARGET QuantLib-Java-jar)
    message(FATAL_ERROR "Scala examples require the Java binding (QuantLib-Java-jar target).")
endif()

set(QUANTLIB_JAR "${CMAKE_CURRENT_BINARY_DIR}/../Java/QuantLib.jar")
set(SCALA_BIN_DIR "${CMAKE_CURRENT_BINARY_DIR}/examples")

file(MAKE_DIRECTORY "${SCALA_BIN_DIR}")

# Compile each Scala example
set(SCALA_CLASS_FILES)
foreach(_example ${SCALA_EXAMPLES})
    set(_src "${CMAKE_CURRENT_SOURCE_DIR}/examples/${_example}.scala")
    set(_class "${SCALA_BIN_DIR}/${_example}.class")

    add_custom_command(
        OUTPUT "${_class}"
        COMMAND "${SCALAC_EXECUTABLE}"
            -cp "${QUANTLIB_JAR}"
            -language:postfixOps
            -d "${SCALA_BIN_DIR}"
            "${_src}"
        DEPENDS "${_src}" QuantLib-Java-jar "${QUANTLIB_JAR}"
        COMMENT "Compiling Scala example: ${_example}"
        VERBATIM
    )
    list(APPEND SCALA_CLASS_FILES "${_class}")
endforeach()

add_custom_target(QuantLib-Scala-examples ALL DEPENDS ${SCALA_CLASS_FILES})

# --- Run targets for each example ---
foreach(_example ${SCALA_EXAMPLES})
    add_custom_target(run-scala-${_example}
        COMMAND ${CMAKE_COMMAND} -E env
            "LD_LIBRARY_PATH=${CMAKE_CURRENT_BINARY_DIR}/../Java"
            "${SCALA_EXECUTABLE}" -cp ".:${QUANTLIB_JAR}:${SCALA_BIN_DIR}"
            "examples.${_example}"
        DEPENDS QuantLib-Scala-examples QuantLibJNI
        WORKING_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}"
        COMMENT "Running Scala example: ${_example}"
        VERBATIM
    )
endforeach()
