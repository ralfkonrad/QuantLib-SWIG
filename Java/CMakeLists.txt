# Java binding for QuantLib-SWIG
#
# Produces:
#   - libQuantLibJNI.so (.jnilib on macOS) — JNI shared library
#   - QuantLib.jar — Java class archive

find_package(Java REQUIRED COMPONENTS Development Runtime)
find_package(JNI REQUIRED)
include(UseJava)

# Mirrors `JAVAC_FLAGS ?= -O -g -source 8 -target 8` in Java/Makefile.am
set(QUANTLIB_SWIG_JAVAC_FLAGS "-O;-g;-source;8;-target;8" CACHE STRING
    "Flags passed to javac when compiling the generated wrappers and examples")

# --- SWIG flags for Java ---
set(JAVA_SWIG_FLAGS "")
if (QUANTLIB_SWIG_JAVA_AUTOLOAD)
    list(APPEND JAVA_SWIG_FLAGS "-DJAVA_AUTOLOAD")
endif()
if (QUANTLIB_SWIG_JAVA_FINALIZER)
    list(APPEND JAVA_SWIG_FLAGS "-DJAVA_FINALIZER")
endif()
if (QUANTLIB_SWIG_JAVA_AUTOCLOSEABLE)
    list(APPEND JAVA_SWIG_FLAGS "-DJAVA_AUTOCLOSEABLE")
endif()

# Output directories for SWIG-generated files
set(JAVA_SWIG_OUTDIR "${CMAKE_CURRENT_BINARY_DIR}/org/quantlib")
set(JAVA_SWIG_CXXDIR "${CMAKE_CURRENT_BINARY_DIR}")

file(MAKE_DIRECTORY "${JAVA_SWIG_OUTDIR}")

# Configure the SWIG source file. Source properties are directory-scoped, so
# these settings apply to this binding only.
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY CPLUSPLUS ON)
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY USE_TARGET_INCLUDE_DIRECTORIES ON)
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY USE_SWIG_DEPENDENCIES TRUE)
# UseSWIG_MODULE_VERSION 2 stages the generated .java files through a private
# directory and removes the previous run's output before regenerating, so a
# wrapper class that is no longer generated cannot linger and end up in the jar.
# That is what `rm -f org/quantlib/*.java` does in Java/Makefile.am.
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY COMPILE_OPTIONS
    -package org.quantlib ${JAVA_SWIG_FLAGS} ${QUANTLIB_SWIG_FLAGS_LIST})
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY
    SWIG_MODULE_NAME QuantLib)

# Use SWIG to generate JNI wrapper
swig_add_library(QuantLibJNI
    TYPE MODULE
    LANGUAGE java
    OUTPUT_DIR "${JAVA_SWIG_OUTDIR}"
    OUTFILE_DIR "${JAVA_SWIG_CXXDIR}"
    SOURCES "${QUANTLIB_SWIG_INTERFACE}")

target_include_directories(QuantLibJNI PRIVATE ${JNI_INCLUDE_DIRS})
target_link_libraries(QuantLibJNI PRIVATE QuantLib::QuantLib)
target_compile_options(QuantLibJNI PRIVATE ${QUANTLIB_SWIG_CXX_WARNING_FLAGS})

# Match the autotools output name (libQuantLibJNI.so / .jnilib)
set_target_properties(QuantLibJNI PROPERTIES
    OUTPUT_NAME "QuantLibJNI"
    PREFIX "lib"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}"
)

# On macOS, JNI libraries use the .jnilib extension
if (APPLE)
    set_target_properties(QuantLibJNI PROPERTIES SUFFIX ".jnilib")
endif()

# --- Build QuantLib.jar ---
set(QUANTLIB_JAR "${CMAKE_CURRENT_BINARY_DIR}/QuantLib.jar")
set(JAVA_BIN_DIR "${CMAKE_CURRENT_BINARY_DIR}/java-bin")

add_custom_command(
    OUTPUT "${QUANTLIB_JAR}"
    COMMAND ${CMAKE_COMMAND}
        -DJAVA_COMPILER=${Java_JAVAC_EXECUTABLE}
        -DJAVA_SOURCE_DIR=${JAVA_SWIG_OUTDIR}
        -DJAVA_BIN_DIR=${JAVA_BIN_DIR}
        -DJAVA_JAR_EXECUTABLE=${Java_JAR_EXECUTABLE}
        -DJAR_OUTPUT=${QUANTLIB_JAR}
        "-DJAVAC_FLAGS=${QUANTLIB_SWIG_JAVAC_FLAGS}"
        -P "${PROJECT_SOURCE_DIR}/cmake/BuildJar.cmake"
    DEPENDS QuantLibJNI
    WORKING_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}"
    COMMENT "Building QuantLib.jar"
    VERBATIM
)

add_custom_target(QuantLib-Java-jar ALL DEPENDS "${QUANTLIB_JAR}")

# --- Examples, used as the test suite (mirrors check-local) ---
set(JAVA_EXAMPLES
    Bonds
    CallableBonds
    CallableBondsOAS
    DiscreteHedging
    EquityOptions
    FRA
    FunctionDelegates
    Time
)

set(JAVA_EXAMPLES_BIN_DIR "${CMAKE_CURRENT_BINARY_DIR}/examples-bin")

set(JAVA_EXAMPLE_CLASSES)
foreach(_example ${JAVA_EXAMPLES})
    set(_src "${CMAKE_CURRENT_SOURCE_DIR}/examples/${_example}.java")
    set(_class "${JAVA_EXAMPLES_BIN_DIR}/examples/${_example}.class")

    add_custom_command(
        OUTPUT "${_class}"
        COMMAND ${CMAKE_COMMAND} -E make_directory "${JAVA_EXAMPLES_BIN_DIR}"
        COMMAND "${Java_JAVAC_EXECUTABLE}" -cp "${QUANTLIB_JAR}"
            ${QUANTLIB_SWIG_JAVAC_FLAGS} -d "${JAVA_EXAMPLES_BIN_DIR}" "${_src}"
        DEPENDS "${_src}" "${QUANTLIB_JAR}"
        COMMENT "Compiling Java example: ${_example}"
        VERBATIM
    )
    list(APPEND JAVA_EXAMPLE_CLASSES "${_class}")

    add_test(NAME java-example-${_example}
        COMMAND "${Java_JAVA_EXECUTABLE}"
                -cp "${JAVA_EXAMPLES_BIN_DIR}${JAVA_CLASSPATH_SEPARATOR}${QUANTLIB_JAR}"
                examples.${_example})
    set_tests_properties(java-example-${_example} PROPERTIES
        ENVIRONMENT "LD_LIBRARY_PATH=${CMAKE_CURRENT_BINARY_DIR};DYLD_LIBRARY_PATH=${CMAKE_CURRENT_BINARY_DIR}")
endforeach()

add_custom_target(QuantLib-Java-examples ALL DEPENDS ${JAVA_EXAMPLE_CLASSES})

# --- Installation ---
install(TARGETS QuantLibJNI
    LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
    RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR})

install(FILES "${QUANTLIB_JAR}"
    DESTINATION ${CMAKE_INSTALL_LIBDIR})
