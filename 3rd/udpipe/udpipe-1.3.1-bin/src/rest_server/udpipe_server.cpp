// This file is part of UDPipe <http://github.com/ufal/udpipe/>.
//
// Copyright 2016 Institute of Formal and Applied Linguistics, Faculty of
// Mathematics and Physics, Charles University in Prague, Czech Republic.
//
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

#include <fstream>
#include <sstream>

#include "common.h"
#include "udpipe_service.h"
#include "utils/iostreams.h"
#include "utils/options.h"
#include "utils/parse_int.h"
#include "utils/path_from_utf8.h"
#include "version/version.h"

using namespace ufal::udpipe;

// On Linux define streambuf writing to syslog
#ifdef __linux__
#include <streambuf>
#include <syslog.h>
#include <unistd.h>

class syslog_streambuf : public streambuf {
 public:
  virtual ~syslog_streambuf() {
    syslog_write();
  }

 protected:
  virtual int overflow(int c) override {
    if (c != EOF && c != '\n')
      buffer.push_back(c);
    else
      syslog_write();
    return c;
  }

 private:
  string buffer;

  void syslog_write() {
    if (!buffer.empty()) {
      syslog(LOG_ERR, "%s", buffer.c_str());
      buffer.clear();
    }
  }
};
#endif

microrestd::rest_server server;

udpipe_service service;
udpipe_service::service_options service_options;

int main(int argc, char* argv[]) {
  iostreams_init();

  options::map options;
  if (!options::parse({{"concurrent_models", options::value::any},
                       {"connection_timeout", options::value::any},
                       {"daemon", options::value::none},
                       {"log_file", options::value::any},
                       {"log_request_max_size", options::value::any},
                       {"no_check_models_loadable",options::value::none},
                       {"no_preload_default",options::value::none},
                       {"max_connections", options::value::any},
                       {"max_request_size", options::value::any},
                       {"threads", options::value::any},
                       {"version", options::value::none},
                       {"help", options::value::none}}, argc, argv, options) ||
      options.count("help") ||
      ((argc < 3 || (argc % 3) != 0) && !options.count("version")))
    runtime_failure("Usage: " << argv[0] << " [options] port default_model_id (model_ids model_file acknowledgements)+\n"
                    "Options: --concurrent_models=maximum concurrently loaded models (default 10)\n"
                    "         --connection_timeout=maximum connection timeout [s] (default 60)\n"
                    "         --daemon (daemonize after start)\n"
                    "         --log_file=file path (no logging if empty, default udpipe_server.log)\n"
                    "         --log_request_max_size=max req log size [kB] (0 unlimited, default 64)\n"
                    "         --max_connections=maximum network connections (default 256)\n"
                    "         --max_request_size=maximum request size [kB] (default 1024)\n"
                    "         --no_check_models_loadable (do not check models are loadable)\n"
                    "         --no_preload_default (do not preload default model)\n"
                    "         --threads=threads to use (default 0 means unlimitted)\n"
                    "         --version\n"
                    "         --help");
  if (options.count("version")) {
    ostringstream other_libraries;
    auto microrestd = microrestd::version::current();
    other_libraries << "MicroRestD " << microrestd.major << '.' << microrestd.minor << '.' << microrestd.patch;
    return cout << version::version_and_copyright(other_libraries.str()) << endl, 0;
  }

  // Process options
  int port = parse_int(argv[1], "port number");
  service_options.default_model = argv[2];
  service_options.concurrent_limit = options.count("concurrent_models") ? parse_int(options["concurrent_models"], "concurrent models") : 10;
  int connection_timeout = options.count("connection_timeout") ? parse_int(options["connection_timeout"], "connection timeout") : 60;
  int log_request_max_size = options.count("log_request_max_size") ? parse_int(options["log_request_max_size"], "log request maximum size") : 64;
  int max_connections = options.count("max_connections") ? parse_int(options["max_connections"], "maximum connections") : 256;
  int max_request_size = options.count("max_request_size") ? parse_int(options["max_request_size"], "maximum request size") : 1024;
  service_options.check_models_loadable = !options.count("no_check_models_loadable");
  service_options.preload_default = !options.count("no_preload_default");
  int threads = options.count("threads") ? parse_int(options["threads"], "number of threads") : 0;

#ifndef __linux__
  if (options.count("daemon")) runtime_failure("The --daemon option is currently supported on Linux only!");
#endif

  // Initialize the service
  for (int i = 3; i < argc; i += 3)
    service_options.model_descriptions.emplace_back(argv[i], argv[i + 1], argv[i + 2]);

  if (!service.init(service_options))
    runtime_failure("Cannot load specified models!");

  // Open log file, if requested
  ofstream log_file;
  string log_file_name = options.count("log_file") ? options["log_file"] : string(argv[0]) + ".log";
  if (!log_file_name.empty()) {
    log_file.open(path_from_utf8(log_file_name).c_str(), ofstream::app);
    if (!log_file) runtime_failure("Cannot open log file '" << log_file_name << "' for writing!");
  }

  // Daemonize if requested
#ifdef __linux__
  if (options.count("daemon")) {
    if (daemon(1, 0) != 0)
      runtime_failure("Cannot daemonize in '" << argv[0] << "' executable!");

    // Redirect cerr to syslog
    openlog("udpipe_server", 0, LOG_USER);
    static syslog_streambuf syslog;
    cerr.rdbuf(&syslog);
  }
#endif

  // Start the server
  if (!log_file_name.empty())
    server.set_log_file(&log_file, log_request_max_size << 10);
  server.set_max_connections(max_connections);
  server.set_max_request_body_size(max_request_size << 10);
  server.set_min_generated(32 << 10);
  server.set_threads(threads);
  server.set_timeout(connection_timeout);

  if (!server.start(&service, port))
    runtime_failure("Cannot start udpipe_server'!");

  cerr << "Successfully started udpipe_server on port " << port << "." << endl;

  // Wait until finished
  server.wait_until_signalled();
  server.stop();

  return 0;
}
