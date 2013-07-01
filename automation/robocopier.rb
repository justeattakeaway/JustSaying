class Robocopier
  include Rake::DSL
  STANDARD_DEPLOY_INTO_OPTIONS = '/e /xd .svn'
  STANDARD_REPLACE_OPTIONS = '/mir /xd .svn'

  def copy(src, dest, opts={})
    files = opts[:files] || '*.*'
    copy_cmd = "robocopy #{src} #{dest} #{files} /S /nfl /ns /ndl /eta #{exclude_dirs(opts)} #{exclude_files(opts)} #{replace_pattern(opts)}"
    sh copy_cmd do |ok, result|
      puts "robocopy: #{ok}, #{result}"
      puts "Robocopy: No errors occurred, and no copying was done - source and destination are identical already" if result.exitstatus == 0
      puts "Robocopy: One or more files copied successfully (that is, new files have arrived)" if result.exitstatus & 1 == 1
      puts "Robocopy: Some extra files or directories were detected.  Examine the log for details" if result.exitstatus & 2 == 1
      puts "Robocopy: Some mismatched files or directories were detected" if result.exitstatus & 4 == 1
      puts "Robocopy: Some files or directories could not be copied" if result.exitstatus & 8 == 1
      puts "Robocopy: Serious error. Robocopy did not copy any files." if result.exitstatus & 16 == 1
      raise "Robocopy error" if result.exitstatus > 8
    end
  end

  def exclude_dirs(opts={})
    xd = opts[:exclude_dirs] || []
    xd.map { |x| "/xd #{x}" }.join(' ')
  end

  def exclude_files(opts={})
    xf = opts[:exclude_files] || []
    xf.map { |x| "/xf #{x}" }.join(' ')
  end

  def replace_pattern(opts={})
    case opts[:deploy_pattern] || opts[:replace_pattern]
      when :clean, :replace then
        STANDARD_REPLACE_OPTIONS
      when :deploy_into, :leave then
        STANDARD_DEPLOY_INTO_OPTIONS
      else
        ''
    end
  end
end
