# Python binding for QuantLib-SWIG
#
# Produces:
#   - _QuantLib.so (.pyd on Windows) — Python extension module
#   - QuantLib.py — Python wrapper module
#
# The built module is placed alongside the existing Python package structure
# so it can be used directly or packaged into a wheel via `python -m build`.

find_package(Python3 REQUIRED COMPONENTS Interpreter Development)

# Output directories for SWIG-generated files
set(PYTHON_SWIG_OUTDIR "${CMAKE_CURRENT_BINARY_DIR}/src/QuantLib")
set(PYTHON_SWIG_CXXDIR "${CMAKE_CURRENT_BINARY_DIR}/src/QuantLib")

file(MAKE_DIRECTORY "${PYTHON_SWIG_OUTDIR}")

# Configure the SWIG source file
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY CPLUSPLUS ON)
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY USE_TARGET_INCLUDE_DIRECTORIES ON)
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY
    SWIG_MODULE_NAME QuantLib)

# Use SWIG to generate Python wrapper
swig_add_library(QuantLib-Python
    TYPE MODULE
    LANGUAGE python
    OUTPUT_DIR "${PYTHON_SWIG_OUTDIR}"
    OUTFILE_DIR "${PYTHON_SWIG_CXXDIR}"
    SOURCES "${QUANTLIB_SWIG_INTERFACE}")

# The SWIG target for Python gets named _QuantLib (standard Python convention)
set_target_properties(QuantLib-Python PROPERTIES
    OUTPUT_NAME "_QuantLib"
    PREFIX ""
    LIBRARY_OUTPUT_DIRECTORY "${PYTHON_SWIG_OUTDIR}"
)

# On Windows, Python extensions use .pyd
if (WIN32)
    set_target_properties(QuantLib-Python PROPERTIES SUFFIX ".pyd")
endif()

# Link against QuantLib and Python
target_link_libraries(QuantLib-Python PRIVATE
    QuantLib::QuantLib
    Python3::Module)

# Apply warning flags for SWIG-generated code
target_compile_options(QuantLib-Python PRIVATE ${QUANTLIB_SWIG_CXX_WARNING_FLAGS})

# SWIG .i file dependencies
set_property(TARGET QuantLib-Python PROPERTY SWIG_DEPENDS ${SWIG_INTERFACE_FILES})

# --- Copy supporting Python files to build directory ---
# Copy __init__.py so the module is usable from the build tree
add_custom_command(
    TARGET QuantLib-Python POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "${CMAKE_CURRENT_SOURCE_DIR}/src/QuantLib/__init__.py"
        "${PYTHON_SWIG_OUTDIR}/__init__.py"
    COMMENT "Copying Python __init__.py to build directory"
)

# --- Installation ---
# Install into the Python site-packages
execute_process(
    COMMAND "${Python3_EXECUTABLE}" -c
        "import sysconfig; print(sysconfig.get_path('platlib'))"
    OUTPUT_VARIABLE PYTHON_SITE_PACKAGES
    OUTPUT_STRIP_TRAILING_WHITESPACE)

install(TARGETS QuantLib-Python
    LIBRARY DESTINATION "${PYTHON_SITE_PACKAGES}/QuantLib"
    RUNTIME DESTINATION "${PYTHON_SITE_PACKAGES}/QuantLib")

install(FILES
    "${PYTHON_SWIG_OUTDIR}/QuantLib.py"
    "${CMAKE_CURRENT_SOURCE_DIR}/src/QuantLib/__init__.py"
    DESTINATION "${PYTHON_SITE_PACKAGES}/QuantLib")
