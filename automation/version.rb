def version
	main = Pathname.new('version').read.chomp
	return "#{main}.#{ENV['BUILD_NUMBER']}" if is_build_agent?
	main
end
