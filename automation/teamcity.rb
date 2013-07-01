def is_build_agent?
	not ENV['BUILD_NUMBER'].nil?
end
