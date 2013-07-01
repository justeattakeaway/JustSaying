require 'bundler'
Bundler.require :build, :debugging, :cucumber
require 'rake/clean'
require 'pathname'
require './automation/logging.rb'
require './automation/teamcity.rb'
require './automation/version.rb'
require './automation/assembly_info.rb'
require './automation/nuget.rb'
require './automation/robocopier.rb'
include Cocaine

name = ENV['component'] || 'SimplesNotificationStack'
@log = setup_logging(ENV['VERBOSITY'] || 'info')
configuration = ENV['msbuild_configuration'] || 'Release'
cmd_opts = {logger: @log}

directory 'out'

task :directories => 'out'
setup_nuget name: name, configuration: configuration, version: version, restore: false

task :clean do
  package_lib = "out/package/lib"
  rm_rf package_lib if Pathname.new(package_lib).exist?
end

AssemblyInfoGenerator.new(log: @log, version: version).generate
desc 'Bootstrap all build-dependencies'
task :bootstrap => [:assembly_info, :directories]
task :package => [:clean]
task :package => [:nuget]
task :default => [:package]

# bundle exec rake msbuild_configuration=debug