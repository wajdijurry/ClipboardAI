cmake_minimum_required(VERSION 3.14)
project(ClipboardAI.Core VERSION 0.1.0)

# Specify C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Include directories
include_directories(include)

# Source files
file(GLOB_RECURSE SOURCES "src/*.cpp")

# Create shared library
add_library(${PROJECT_NAME} SHARED ${SOURCES})

# Set output directory
set_target_properties(${PROJECT_NAME} PROPERTIES
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
)

# Dependencies (commented out until actually needed)
# find_package(ONNXRuntime REQUIRED)
# target_link_libraries(${PROJECT_NAME} PRIVATE ONNXRuntime::ONNXRuntime)

# find_package(Tesseract REQUIRED)
# target_link_libraries(${PROJECT_NAME} PRIVATE Tesseract::Tesseract)

# Export symbols for DLL
target_compile_definitions(${PROJECT_NAME} PRIVATE CLIPBOARDAI_EXPORTS)

# Install rules
install(TARGETS ${PROJECT_NAME}
    RUNTIME DESTINATION bin
    LIBRARY DESTINATION lib
)
