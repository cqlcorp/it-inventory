import React from 'react'

// Packages

// Components

// Utils
import {concatStyles as s} from '../../../utilities/mikesConcat'

// Styles
import styles from './Input.module.css'

// Types
interface IInputProps {
    label?: string
    value: string
    onChange: any
    maxChars: number
    className?: string
    width?: string
    fontSize?: string
}

// Helpers

// Primary Component
export const Input: React.SFC<IInputProps> = props => {
    const {label, onChange, value, maxChars, className, width = '380px', fontSize = '24px'} = props

    return (
        <div className={s(styles.main, className)}>
            {label && (
                <div className={styles.text} style={{fontSize: `${fontSize}`}}>
                    {label}
                </div>
            )}
            <input
                type='text'
                className={styles.input}
                value={value}
                onChange={e => onChange(e)}
                style={{width: `${width}`}}
            />
            {value && value.length > maxChars && <div className={styles.msg}>Too Many Characters</div>}
        </div>
    )
}
