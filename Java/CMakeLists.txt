# Java binding for QuantLib-SWIG
#
# Produces:
#   - libQuantLibJNI.so (.dylib / .jnilib on macOS) — JNI shared library
#   - QuantLib.jar — Java class archive

find_package(Java REQUIRED COMPONENTS Development)
find_package(JNI REQUIRED)
include(UseJava)

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

# Configure the SWIG source file
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY CPLUSPLUS ON)
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY USE_TARGET_INCLUDE_DIRECTORIES ON)
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY COMPILE_OPTIONS
    -package org.quantlib ${JAVA_SWIG_FLAGS})
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY
    SWIG_MODULE_NAME QuantLib)

# Use SWIG to generate JNI wrapper
swig_add_library(QuantLibJNI
    TYPE MODULE
    LANGUAGE java
    OUTPUT_DIR "${JAVA_SWIG_OUTDIR}"
    OUTFILE_DIR "${JAVA_SWIG_CXXDIR}"
    SOURCES "${QUANTLIB_SWIG_INTERFACE}")

# Add include directories and link libraries
target_include_directories(QuantLibJNI PRIVATE ${JNI_INCLUDE_DIRS})
target_link_libraries(QuantLibJNI PRIVATE QuantLib::QuantLib)

# Apply warning flags for SWIG-generated code
target_compile_options(QuantLibJNI PRIVATE ${QUANTLIB_SWIG_CXX_WARNING_FLAGS})

# SWIG .i file dependencies (so re-swig when any .i changes)
set_property(TARGET QuantLibJNI PROPERTY SWIG_DEPENDS ${SWIG_INTERFACE_FILES})

# Set output name to match autotools (libQuantLibJNI.so / .dylib / .jnilib)
set_target_properties(QuantLibJNI PROPERTIES
    OUTPUT_NAME "QuantLibJNI"
    PREFIX "lib"
    # Remove the default module prefix
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}"
)

# On macOS, JNI libraries use .jnilib extension
if (APPLE)
    set_target_properties(QuantLibJNI PROPERTIES SUFFIX ".jnilib")
endif()

# --- Build QuantLib.jar ---
# We compile all generated .java files into a jar after SWIG runs

# Custom command to compile Java sources and create jar
set(QUANTLIB_JAR "${CMAKE_CURRENT_BINARY_DIR}/QuantLib.jar")
set(JAVA_BIN_DIR "${CMAKE_CURRENT_BINARY_DIR}/java-bin")

add_custom_command(
    OUTPUT "${QUANTLIB_JAR}"
    COMMAND ${CMAKE_COMMAND} -E make_directory "${JAVA_BIN_DIR}"
    COMMAND ${CMAKE_COMMAND}
        -DJAVA_COMPILER=${Java_JAVAC_EXECUTABLE}
        -DJAVA_SOURCE_DIR=${JAVA_SWIG_OUTDIR}
        -DJAVA_BIN_DIR=${JAVA_BIN_DIR}
        -DJAVA_JAR_EXECUTABLE=${Java_JAR_EXECUTABLE}
        -DJAR_OUTPUT=${QUANTLIB_JAR}
        -P ${CMAKE_CURRENT_SOURCE_DIR}/../cmake/BuildJar.cmake
    DEPENDS QuantLibJNI
    WORKING_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}"
    COMMENT "Building QuantLib.jar"
    VERBATIM
)

add_custom_target(QuantLib-Java-jar ALL DEPENDS "${QUANTLIB_JAR}")

# --- Installation ---
install(TARGETS QuantLibJNI
    LIBRARY DESTINATION lib
    RUNTIME DESTINATION bin)

install(FILES "${QUANTLIB_JAR}"
    DESTINATION lib)
