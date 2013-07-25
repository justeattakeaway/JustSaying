def setup_nuget opts={}
	name = opts[:name]
	configuration = opts[:configuration]
	version = opts[:version] || version
	nuget = opts[:nuget_exe] || ".nuget/nuget.exe"
	@log = opts[:log] || Logger.new(STDOUT)
  restore_packages = opts[:restore] || true

	namespace :nuget do
		directory 'packages'
		desc "restore packages"
		task :restore => ['packages'] do
      if (restore_packages)
        FileList.new("**/packages.config").map{|pc|Pathname.new(pc)}.each do |pc|
          restore = CommandLine.new(nuget, "install \"#{pc.to_s.gsub('/', '\\')}\" -source http://ci.dev/guestAuth/app/nuget/v1/FeedService.svc/ -source http://nuget.org/api/v2/ -o packages", logger: @log)
          restore.run
        end
      end
    end

		desc "Harvest the output to prepare package"
		task :harvest

		package_dir = "out/#{name}/package"
		package_lib = "#{package_dir}/lib/net40"
		directory package_dir
		directory package_lib

		task :harvest => [package_lib] do
			lib_files = FileList.new("#{name}/bin/#{configuration}/*.{exe,config,dll,pdb,xml}")
			lib_files.exclude /(Shouldly|Rhino|nunit|Test|Castle|NLog|ServiceStack|AWSSDK)/
			lib_files.map{|f|Pathname.new f}.each do |f|
			harvested = "#{package_lib}/#{f.basename}"
				FileUtils.cp_r f, harvested
			end
		end

		desc "Create the nuspec"
		nuspec :nuspec => [package_dir, :harvest] do |nuspec|
			nuspec.id = name
			nuspec.version = version
			nuspec.authors = "Anton Jefcoate"
			nuspec.owners = "Anton Jefcoate"
			nuspec.description = "Simples Notification Stack is a set of tools and the messages required by team simples for our order fulfilment messaging stack"
			nuspec.summary = "Order fulfilment messaging"
			nuspec.language = "en-GB"
			nuspec.licenseUrl = "https://github.je-labs.com/Simples/#{name}/blob/master/LICENSE.md"
			nuspec.projectUrl = "https://github.je-labs.com/Simples/#{name}"
			nuspec.working_directory = package_dir
			nuspec.output_file = "#{name}.nuspec"
			nuspec.tags = ""
      if (name == "JustEat.Simples.NotificationStack")
        nuspec.dependency "AWSSDK", "1.5.26.3"
        nuspec.dependency "Newtonsoft.Json", "4.5.0.0"
        nuspec.dependency "ServiceStack.Text", "3.9.56"
        nuspec.dependency "NLog", "2.0.1.2"
      elsif (name == "JustEat.Simples.DataAccess")
        nuspec.dependency "Dapper", "1.13"
        nuspec.dependency "NLog", "2.0.1.2"
      elsif (name == "JustEat.Simples.Api")
        nuspec.dependency "Newtonsoft.Json", "4.5.0.0"
        nuspec.dependency "RestSharp", "104.1.0.0"
      end

		end

		nupkg = "#{package_dir}/#{name}.#{version}.nupkg"
		desc "Create the nuget package"
		file nupkg => [:nuspec] do |nugetpack|
      pack = CommandLine.new(nuget, "pack #{package_dir}/#{name}.nuspec -basepath #{package_dir} -o #{package_dir}", logger: @log)
			pack.run
		end
		task :nupkg => nupkg

		task :default => [:harvest, :nuspec, :nupkg]
	end
	task :nuget => 'nuget:default'
end
