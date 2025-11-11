#ifndef TESTLOGGER_H
#define TESTLOGGER_H

#include "colorer/common/Logger.h"

class TestLogger : public Logger
{
 public:
  static constexpr std::string_view LogLevelStr[] {"off", "error", "warning", "info", "debug", "trace"};

  TestLogger() = default;

  ~TestLogger() override = default;

  void log(Logger::LogLevel level, const char* /*filename_in*/, int /*line_in*/, const char* /*funcname_in*/,
           const char* message) override
  {
    if (level > current_level) {
      return;
    }
    // we are not writing anywhere, the very fact of calling the function is important.
    // std::cerr << message << '\n';
  }

  void flush() override {}

  LogLevel getCurrentLogLevel() override { return current_level; }

 private:
  Logger::LogLevel current_level = Logger::LOG_OFF;
};

#endif  // TESTLOGGER_H
