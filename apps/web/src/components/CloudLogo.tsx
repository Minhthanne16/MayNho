export function CloudLogo({ size = 36, className = "" }: { size?: number; className?: string }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 48 48"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      aria-hidden="true"
    >
      {/* Gentle pastel cloud with cute smile */}
      <path
        d="M14 36H35C39.4183 36 43 32.4183 43 28C43 23.7744 39.7252 20.3134 35.5684 20.0242C34.7865 13.2081 29.0142 8 22 8C15.8643 8 10.6698 12.0084 9.06841 17.6534C5.0886 18.5714 2 22.1587 2 26.5C2 31.7467 6.25329 36 11.5 36H14Z"
        fill="#E8DEF8"
      />
      <path
        d="M14 36H35C39.4183 36 43 32.4183 43 28C43 23.7744 39.7252 20.3134 35.5684 20.0242C34.7865 13.2081 29.0142 8 22 8C15.8643 8 10.6698 12.0084 9.06841 17.6534C5.0886 18.5714 2 22.1587 2 26.5C2 31.7467 6.25329 36 11.5 36H14Z"
        stroke="#6750A4"
        strokeWidth="2.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      {/* Cute eyes */}
      <circle cx="18" cy="24" r="2" fill="#252238" />
      <circle cx="28" cy="24" r="2" fill="#252238" />
      {/* Gentle smile */}
      <path
        d="M21 28C21.8 29.2 24.2 29.2 25 28"
        stroke="#252238"
        strokeWidth="1.8"
        strokeLinecap="round"
      />
      {/* Rosy cheeks */}
      <circle cx="15" cy="27" r="1.5" fill="#F8B4C9" />
      <circle cx="31" cy="27" r="1.5" fill="#F8B4C9" />
    </svg>
  );
}
