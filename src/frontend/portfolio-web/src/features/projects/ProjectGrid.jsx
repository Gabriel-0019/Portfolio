import ProjectCard from './ProjectCard.jsx'

export default function ProjectGrid({ projects, headingLevel }) {
  return (
    <ul className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
      {projects.map((project) => (
        <li key={project.slug} className="flex">
          <ProjectCard project={project} headingLevel={headingLevel} />
        </li>
      ))}
    </ul>
  )
}
