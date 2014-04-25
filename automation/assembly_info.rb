require 'json'
require 'rake'
require 'cocaine'

class AssemblyInfoGenerator
  include Rake::DSL
  include Cocaine

  def initialize(params={})
    @projects = params[:projects] || FileList.new("**/*.csproj").map{|p|Pathname.new p}
    @internals_visible_to = params[:internals_visible_to] || nil
    @log = params[:log] || Logger.new(STDOUT)
    @checkout_root = params[:checkout_root] || '.'
    @version = params[:version]
  end

  def generate
    @projects.each {|p| generate_assembly_info_for p}
  end

  def generate_assembly_info_for(project)
    name = project.basename.sub(project.extname, '').to_s
    @log.debug "generate assembly info for #{name}"

    dir = File.join(project.dirname.to_s, 'Properties')
    directory dir
    output = File.join dir, "AssemblyInfo.cs"
    desc "Generate AssemblyInfo.cs for #{name}"
    assemblyinfo :assembly_info => [dir] do |asm|
     asm.version = @version
     asm.company_name = "Just-Eat Holding Ltd"
     asm.title = name
     asm.copyright = "Copyright #{Time.now.year}, by Just-Eat Holding Ltd"
     asm.namespaces = ['System']
     asm.custom_attributes :CLSCompliant => false
     @log.info "IVT: #{@internals_visible_to}"
     if @internals_visible_to
      asm.custom_attributes.merge!({:InternalsVisibleTo => @internals_visible_to})
      asm.namespaces << 'System.Runtime.CompilerServices'
    end
    @log.info "asm:ca = #{asm.custom_attributes}"
    asm.com_visible = false
    asm.description = "JustSaying is a light-weight service bus on top of AWS services that allows communication via messaging in a distributed architecture."
    asm.output_file = output
  end
  CLEAN.include output
end

def read_branch
  case source_control
  when :git then `git branch --no-color`.match(/\* (.*)$/i)[1]
  else raise "Unsupported source control #{scm}"
  end
end

def source_control
  return @scm unless @scm.nil?
  sh "git status" do |ok,result|
   @scm = :git
   return :git if ok
 end
 raise 'Unsupported source control'
end

def read_revision
  case source_control
    when :git then `git log -1 --no-color`.match(/commit (.*)$/i)[1]
    else raise "Unsupported source control #{scm}"
    end
  end
end
