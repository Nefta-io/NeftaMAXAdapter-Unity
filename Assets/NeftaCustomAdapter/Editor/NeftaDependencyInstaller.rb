require 'xcodeproj'

project = Xcodeproj::Project.open('Unity-iPhone.xcodeproj')

project.targets.each do |target|
  if target.name == 'Unity-iPhone'
    target.frameworks_build_phase.files_references.each do |fr|
      if fr.path.include?('NeftaSDK')
        puts 'NeftaSDK is already included'
        exit
      end
    end
      
    xcf = project.new_file('Pods/NeftaSDK/NeftaSDK.xcframework')
    
    embedFramework = project.new(Xcodeproj::Project::Object::PBXCopyFilesBuildPhase)
    embedFramework.name = 'Embed Frameworks'
    embedFramework.symbol_dst_subfolder_spec = :frameworks
    target.build_phases << embedFramework
    
    embedFramework.add_file_reference(xcf)
    
    buildFile = target.frameworks_build_phase.add_file_reference(xcf)
    buildFile.settings = { 'ATTRIBUTES' => ['CodeSignOnCopy', 'RemoveHeadersOnCopy'] }
  end
end

project.save
