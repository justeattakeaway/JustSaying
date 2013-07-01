require 'log4r-color'
include Log4r

def translate_to_log4r(level)
	case level.to_sym
		when :debug then DEBUG
		when :info then INFO
		when :warn then WARN
		when :error then ERROR
		when :fatal then FATAL
		else WARN
	end
end

def setup_logging(level)
	formatter = PatternFormatter.new(:pattern => "%l - %m")
	ColorOutputter.new 'color', {
		:formatter => formatter,
		:colors => {
			:debug => :white, 
			:info => :gr, 
			:warn => :yellow, 
			:error => :red, 
			:fatal => {
				:color => :red, :background => :white
			} 
		} 
	}
	Log4r::Logger.new('color_logger', translate_to_log4r(level)).add('color')
	Log4r::Logger['color_logger']
end

class Log4r::Logger
	def happy(msg)
		before = Log4r::Outputter['color'].colors[:info]
		Log4r::Outputter['color'].colors[:info] = :green
		info(msg)
		Log4r::Outputter['color'].colors[:info] = before
	end
end
