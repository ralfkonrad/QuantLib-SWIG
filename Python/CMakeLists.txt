# Python binding for QuantLib-SWIG
#
# Produces:
#   - _QuantLib.so (.pyd on Windows) — Python extension module
#   - QuantLib.py — Python wrapper module
#
# This builds the extension directly, which is what makes an edit/build/test
# loop and a debugger usable. Producing a *wheel* remains the job of the
# autotools build (`make -C Python`, which drives `python -m build --wheel`
# through setup.py); this file deliberately does not duplicate that.

find_package(Python3 REQUIRED COMPONENTS Interpreter Development)

# Output directories for SWIG-generated files
set(PYTHON_SWIG_OUTDIR "${CMAKE_CURRENT_BINARY_DIR}/src/QuantLib")
set(PYTHON_SWIG_CXXDIR "${CMAKE_CURRENT_BINARY_DIR}/src/QuantLib")

file(MAKE_DIRECTORY "${PYTHON_SWIG_OUTDIR}")

# Configure the SWIG source file. Source properties are directory-scoped, so
# these settings apply to this binding only.
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY CPLUSPLUS ON)
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY USE_TARGET_INCLUDE_DIRECTORIES ON)
# Let swig itself report the .i files it includes, instead of globbing SWIG/*.i
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY USE_SWIG_DEPENDENCIES TRUE)
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY
    SWIG_MODULE_NAME QuantLib)
if (QUANTLIB_SWIG_FLAGS_LIST)
    set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY COMPILE_OPTIONS
        ${QUANTLIB_SWIG_FLAGS_LIST})
endif()

# Use SWIG to generate Python wrapper
swig_add_library(QuantLib-Python
    TYPE MODULE
    LANGUAGE python
    OUTPUT_DIR "${PYTHON_SWIG_OUTDIR}"
    OUTFILE_DIR "${PYTHON_SWIG_CXXDIR}"
    SOURCES "${QUANTLIB_SWIG_INTERFACE}")

# The module must be named _QuantLib (standard Python convention)
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

# --- Copy supporting Python files to build directory ---
# Copy __init__.py so the module is importable from the build tree
add_custom_command(
    TARGET QuantLib-Python POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "${CMAKE_CURRENT_SOURCE_DIR}/src/QuantLib/__init__.py"
        "${PYTHON_SWIG_OUTDIR}/__init__.py"
    COMMENT "Copying Python __init__.py to build directory"
)

# --- Tests ---
# Mirrors `make -C Python check`, which runs `tox run` -> `pytest test`.
# Running pytest directly keeps the build tree as the import source, so the
# test exercises exactly what was just compiled.
add_test(NAME python-tests
    COMMAND "${Python3_EXECUTABLE}" -m pytest -c "${CMAKE_CURRENT_SOURCE_DIR}/pytest.ini"
            "${CMAKE_CURRENT_SOURCE_DIR}/test"
)
set_tests_properties(python-tests PROPERTIES
    ENVIRONMENT "PYTHONPATH=${CMAKE_CURRENT_BINARY_DIR}/src")

# pytest is not a build dependency, so a missing one must not be reported as a
# test failure. Disable the test instead, and say why.
execute_process(
    COMMAND "${Python3_EXECUTABLE}" -c "import pytest"
    RESULT_VARIABLE _pytest_missing
    OUTPUT_QUIET ERROR_QUIET)
if (_pytest_missing)
    set_tests_properties(python-tests PROPERTIES DISABLED TRUE)
    message(STATUS
        "pytest not available for ${Python3_EXECUTABLE}; the python-tests test "
        "is disabled. Install pytest and re-run cmake to enable it.")
endif()

# --- Installation ---
# Default to a prefix-relative location rather than the interpreter's
# site-packages: a plain `cmake --install` must not overwrite a pip-managed
# QuantLib. Point this at Python3_SITEARCH explicitly to install into the
# interpreter instead.
set(QUANTLIB_SWIG_PYTHON_INSTALL_DIR
    "${CMAKE_INSTALL_LIBDIR}/python${Python3_VERSION_MAJOR}.${Python3_VERSION_MINOR}/site-packages"
    CACHE STRING "Installation directory for the Python module (relative to CMAKE_INSTALL_PREFIX, or absolute)")

install(TARGETS QuantLib-Python
    LIBRARY DESTINATION "${QUANTLIB_SWIG_PYTHON_INSTALL_DIR}/QuantLib"
    RUNTIME DESTINATION "${QUANTLIB_SWIG_PYTHON_INSTALL_DIR}/QuantLib")

install(FILES
    "${PYTHON_SWIG_OUTDIR}/QuantLib.py"
    "${CMAKE_CURRENT_SOURCE_DIR}/src/QuantLib/__init__.py"
    DESTINATION "${QUANTLIB_SWIG_PYTHON_INSTALL_DIR}/QuantLib")
