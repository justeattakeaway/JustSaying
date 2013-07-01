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
setup_nuget name: name, configuration: configuration, version: version

task :harvest do
  src_destination = "out/package/lib/net40"
  excludeFiles = ["AWSSDK.dll"]
  exclude_dirs = [""]

  rm_rf src_destination if Pathname.new(src_destination).exist?
  r = Robocopier.new
  r.copy(File.join("AwsTools", "/bin/#{configuration}"), src_destination, {:exclude_files => excludeFiles, :exclude_dirs => exclude_dirs})
end

AssemblyInfoGenerator.new(log: @log, version: version).generate
desc 'Bootstrap all build-dependencies'
task :bootstrap => [:assembly_info, :directories]
task :package => [:harvest]
task :package => [:bootstrap]
task :package => [:nuget]
task :default => [:package]

# bundle exec rake msbuild_configuration=debug