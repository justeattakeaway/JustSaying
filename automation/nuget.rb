def setup_nuget opts={}
	name = opts[:name]
	configuration = opts[:configuration]
	version = opts[:version] || version
	nuget = opts[:nuget_exe] || ".nuget/nuget.exe"
	@log = opts[:log] || Logger.new(STDOUT)

	namespace :nuget do
		directory 'packages'
		desc "restore packages"
		task :restore => ['packages'] do
			FileList.new("**/packages.config").map{|pc|Pathname.new(pc)}.each do |pc|
        restore = CommandLine.new(nuget, "install \"#{pc.to_s.gsub('/', '\\')}\" -source http://ci.dev/guestAuth/app/nuget/v1/FeedService.svc/ -source http://nuget.org/api/v2/ -o packages", logger: @log)
				restore.run
			end
		end

		desc "Harvest the output to prepare package"
		task :harvest

		package_dir = "out/package"
		package_lib = "#{package_dir}/lib/net40"
		directory package_dir
		directory package_lib

		task :harvest => [package_lib] do
			lib_files = FileList.new("src/JustPay/bin/#{configuration}/*.{exe,config,dll,pdb,xml}")
			lib_files.exclude /(Shouldly|Rhino|nunit|Test|Castle|NLog|ServiceStack)/
			lib_files.map{|f|Pathname.new f}.each do |f|
			harvested = "#{package_lib}/#{f.basename}"
				FileUtils.cp_r f, harvested
			end
		end

		desc "Create the nuspec"
		nuspec :nuspec => [package_dir, :harvest] do |nuspec|
			nuspec.id = name
			nuspec.version = version
			nuspec.authors = "Peter Mounce"
			nuspec.owners = "Peter Mounce"
			nuspec.description = "JustPay is our library defining the request/response contract to our payments API"
			nuspec.summary = "JustPay is our payments API contract"
			nuspec.language = "en-GB"
			nuspec.licenseUrl = "https://github.je-labs.com/PRS/#{name}/blob/master/LICENSE.md"
			nuspec.projectUrl = "https://github.je-labs.com/PRS/#{name}"
			nuspec.working_directory = package_dir
			nuspec.output_file = "#{name}.nuspec"
			nuspec.tags = "payment client"
      nuspec.dependency "ServiceStack.Common", "3.9.49"
		end

		nupkg = "out/#{name}.#{version}.nupkg"
		desc "Create the nuget package"
		file nupkg => [:nuspec] do |nugetpack|
      pack = CommandLine.new(nuget, "pack out\\package\\#{name}.nuspec -basepath out\\package -o out", logger: @log)
			pack.run
		end
		task :nupkg => nupkg

		task :default => [:harvest, :nuspec, :nupkg]
	end
	task :nuget => 'nuget:default'
end
