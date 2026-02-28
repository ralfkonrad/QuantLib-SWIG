# R binding for QuantLib-SWIG
#
# Produces:
#   - QuantLib.so — R shared library (in src/)
#   - QuantLib.R — R wrapper (in R/)
#
# Uses R CMD check/install for building and testing, matching the
# autotools approach. The SWIG step is done via CMake, then R's own
# build system takes over for compilation and packaging.

# Find R
find_program(R_EXECUTABLE R)
if (NOT R_EXECUTABLE)
    message(FATAL_ERROR "R not found. Please install R or set R_EXECUTABLE.")
endif()
message(STATUS "Found R: ${R_EXECUTABLE}")

# Output directories for SWIG-generated files
set(R_SWIG_SRCDIR "${CMAKE_CURRENT_BINARY_DIR}/src")
set(R_SWIG_RDIR "${CMAKE_CURRENT_BINARY_DIR}/R")

file(MAKE_DIRECTORY "${R_SWIG_SRCDIR}")
file(MAKE_DIRECTORY "${R_SWIG_RDIR}")

# Configure the SWIG source file
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY CPLUSPLUS ON)
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY USE_TARGET_INCLUDE_DIRECTORIES ON)
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY
    SWIG_MODULE_NAME QuantLib)

# Use SWIG to generate R wrapper
# Note: R packages have their own build system (R CMD), so we generate
# the wrapper C++ and R files, then let R CMD handle compilation.
swig_add_library(QuantLib-R
    TYPE MODULE
    LANGUAGE r
    OUTPUT_DIR "${R_SWIG_SRCDIR}"
    OUTFILE_DIR "${R_SWIG_SRCDIR}"
    SOURCES "${QUANTLIB_SWIG_INTERFACE}")

# Link against QuantLib
target_link_libraries(QuantLib-R PRIVATE QuantLib::QuantLib)

# Apply warning flags for SWIG-generated code
target_compile_options(QuantLib-R PRIVATE ${QUANTLIB_SWIG_CXX_WARNING_FLAGS})

# SWIG .i file dependencies
set_property(TARGET QuantLib-R PROPERTY SWIG_DEPENDS ${SWIG_INTERFACE_FILES})

# R expects the .so in src/ and .R in R/
set_target_properties(QuantLib-R PROPERTIES
    OUTPUT_NAME "QuantLib"
    PREFIX ""
    LIBRARY_OUTPUT_DIRECTORY "${R_SWIG_SRCDIR}"
)

# Move the generated .R file to the R/ subdirectory (SWIG puts it next to the .cpp)
add_custom_command(
    TARGET QuantLib-R POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E rename
        "${R_SWIG_SRCDIR}/QuantLib.R"
        "${R_SWIG_RDIR}/QuantLib.R"
    COMMENT "Moving QuantLib.R to R/ directory"
    VERBATIM
)

# --- R CMD check target (optional) ---
add_custom_target(QuantLib-R-check
    COMMAND "${R_EXECUTABLE}" CMD check "${CMAKE_CURRENT_SOURCE_DIR}"
    DEPENDS QuantLib-R
    WORKING_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/.."
    COMMENT "Running R CMD check"
    VERBATIM
)

# --- R CMD INSTALL target (optional) ---
add_custom_target(QuantLib-R-install
    COMMAND "${R_EXECUTABLE}" CMD INSTALL "${CMAKE_CURRENT_SOURCE_DIR}"
    DEPENDS QuantLib-R
    WORKING_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}"
    COMMENT "Running R CMD INSTALL"
    VERBATIM
)
